using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;

namespace DroidSeekArc
{

    /**
     * 
     * 
     * This is a class that functions much like a SeekBar but
     * follows a circle path instead of a straight line.
     * 
     * @author Neil Davies
     * 
     */
    public class SeekArc : View
    {
        #region Private Fields

        private const string TAG = "SeekArc";
        private static readonly int InvalidProgressValue = -1;
        // The initial rotational offset -90 means we start at 12 o'clock
        private const int MAngleOffset = -90;

        /**
         * The Drawable for the seek arc thumbnail
         */
        private Drawable _thumb;

        /**
         * The Maximum value that this SeekArc can be set to
         */
        private int _max = 100;

        // Internal variables
        private int _arcRadius;
        private float _progressSweep;
        private readonly RectF _arcRect = new RectF();
        private Paint _arcPaint;
        private Paint _progressPaint;
        private int _translateX;
        private int _translateY;
        private int _thumbXPos;
        private int _thumbYPos;
        private double _touchAngle;
        private float _touchIgnoreRadius;

        #endregion

        #region Events
        /// <summary>
        /// Notification that the progress level has changed. Clients can use the
        /// fromUser parameter to distinguish user-initiated changes from those
        /// that occurred programmatically.
        /// 
        /// @param seekArc
        ///            The SeekArc whose progress has changed
        /// @param progress
        ///            The current progress level. This will be in the range
        ///            0..max where max was set by
        ///            {@link ProgressArc#setMax(int)}. (The default value for
        ///            max is 100.)
        /// @param fromUser
        ///            True if the progress change was initiated by the user.
        /// </summary>
        public event EventHandler<SeekArcProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Notification that the user has started a touch gesture. Clients may
        /// want to use this to disable advancing the seekbar.
        /// 
        /// @param seekArc
        ///            The SeekArc in which the touch gesture began
        /// </summary>
        public event EventHandler<SeekArcTrackingTouchEventArgs> StartTrackingTouch;

        /// <summary>
        ///  
        /// Notification that the user has finished a touch gesture. Clients may
        /// want to use this to re-enable advancing the seekarc.
        /// 
        /// @param seekArc
        ///            The SeekArc in which the touch gesture began
        /// </summary>
        public event EventHandler<SeekArcTrackingTouchEventArgs> StopTrackingTouch;
        #endregion

        #region Public Properties
        /**
         * The Current value that the SeekArc is set to
         */
        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set
            {
                UpdateProgress(value, false);
            }
        }

        /**
         * The width of the progress line for this SeekArc
         */
        private int _progressWidth = 4;
        public int ProgressWidth
        {
            get { return _progressWidth; }
            set
            {
                _progressWidth = value;
                _progressPaint.StrokeWidth = value;
            }
        }

        /**
         * The Width of the background arc for the SeekArc 
         */
        private int _arcWidth = 2;
        public int ArcWidth
        {
            get { return _arcWidth; }
            set
            {
                _arcWidth = value;
                _arcPaint.StrokeWidth = value;
            }
        }

        /**
         * The Angle to start drawing this Arc from
         */
        private int _startAngle;
        public int StartAngle
        {
            get { return _startAngle; }
            set
            {
                _startAngle = value;
                UpdateThumbPosition();
            }
        }

        /**
         * The Angle through which to draw the arc (Max is 360)
         */
        private int _sweepAngle = 360;
        public int SweepAngle
        {
            get { return _sweepAngle; }
            set
            {
                _sweepAngle = value;
                UpdateThumbPosition();
            }
        }

        /**
         * The rotation of the SeekArc- 0 is twelve o'clock
         */
        private int _arcRotation;
        public int ArcRotation
        {
            get { return _arcRotation; }
            set
            {
                _arcRotation = value;
                UpdateThumbPosition();
            }
        }

        /**
         * Give the SeekArc rounded edges
         */
        private bool _hasRoundedEdges;
        public bool HasRoundedEdges
        {
            get { return _hasRoundedEdges; }
            set
            {
                _hasRoundedEdges = value;
                if (_hasRoundedEdges)
                {
                    _arcPaint.StrokeCap = Paint.Cap.Round;
                    _progressPaint.StrokeCap = Paint.Cap.Round;
                }
                else
                {
                    _arcPaint.StrokeCap = Paint.Cap.Square;
                    _progressPaint.StrokeCap = Paint.Cap.Square;
                }
            }
        }

        /**
         * Enable touch inside the SeekArc
         */
        private bool _isTouchInsideEnabled = true;
        public bool IsTouchInsideEnabled
        {
            get { return _isTouchInsideEnabled; }
            set
            {
                _isTouchInsideEnabled = value;

                var thumbHalfheight = _thumb.IntrinsicHeight / 2;
                var thumbHalfWidth = _thumb.IntrinsicWidth / 2;

                if (_isTouchInsideEnabled)
                {
                    _touchIgnoreRadius = (float)_arcRadius / 4;
                }
                else
                {
                    // Don't use the exact radius makes interaction too tricky
                    _touchIgnoreRadius = _arcRadius
                            - Math.Min(thumbHalfWidth, thumbHalfheight);
                }
            }
        }

        /**
         * Will the progress increase clockwise or anti-clockwise
         */
        private bool _clockwise = true;
        public bool Clockwise
        {
            get { return _clockwise; }
            set { _clockwise = value; }
        }

        #endregion



        public SeekArc(Context context)
            : base(context)
        {

            Init(context, null, 0);
        }

        public SeekArc(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init(context, attrs, Resource.Attribute.seekArcStyle);
        }

        public SeekArc(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Init(context, attrs, defStyle);
        }


        private void Init(Context context, IAttributeSet attrs, int defStyle)
        {

            Log.Debug(TAG, "Initialising SeekArc");
            var density = context.Resources.DisplayMetrics.Density;

            // Defaults, may need to link this into theme settings
            var arcColor = Resources.GetColor(Resource.Color.progress_gray);
            var progressColor = Resources.GetColor(Android.Resource.Color.HoloBlueLight);
            _thumb = Resources.GetDrawable(Resource.Drawable.seek_arc_control_selector);
            // Convert progress width to pixels for current density
            _progressWidth = (int)(_progressWidth * density);


            if (attrs != null)
            {
                // Attribute initialization
                var a = context.ObtainStyledAttributes(attrs,
                        Resource.Styleable.SeekArc, defStyle, 0);

                var thumb = a.GetDrawable(Resource.Styleable.SeekArc_thumb);
                if (thumb != null)
                {
                    _thumb = thumb;
                }



                var thumbHalfheight = (int)_thumb.IntrinsicHeight / 2;
                var thumbHalfWidth = (int)_thumb.IntrinsicWidth / 2;
                _thumb.SetBounds(-thumbHalfWidth, -thumbHalfheight, thumbHalfWidth,
                        thumbHalfheight);

                _max = a.GetInteger(Resource.Styleable.SeekArc_max, _max);
                _progress = a.GetInteger(Resource.Styleable.SeekArc_progress, _progress);
                _progressWidth = (int)a.GetDimension(
                        Resource.Styleable.SeekArc_progressWidth, _progressWidth);
                _arcWidth = (int)a.GetDimension(Resource.Styleable.SeekArc_arcWidth,
                        _arcWidth);
                _startAngle = a.GetInt(Resource.Styleable.SeekArc_startAngle, _startAngle);
                _sweepAngle = a.GetInt(Resource.Styleable.SeekArc_sweepAngle, _sweepAngle);
                _arcRotation = a.GetInt(Resource.Styleable.SeekArc_rotation, _arcRotation);
                _hasRoundedEdges = a.GetBoolean(Resource.Styleable.SeekArc_roundEdges,
                        _hasRoundedEdges);
                _isTouchInsideEnabled = a.GetBoolean(Resource.Styleable.SeekArc_touchInside,
                        _isTouchInsideEnabled);
                _clockwise = a.GetBoolean(Resource.Styleable.SeekArc_clockwise,
                        _clockwise);

                arcColor = a.GetColor(Resource.Styleable.SeekArc_arcColor, arcColor);
                progressColor = a.GetColor(Resource.Styleable.SeekArc_progressColor,
                        progressColor);

                a.Recycle();
            }

            _progress = (_progress > _max) ? _max : _progress;
            _progress = (_progress < 0) ? 0 : _progress;

            _sweepAngle = (_sweepAngle > 360) ? 360 : _sweepAngle;
            _sweepAngle = (_sweepAngle < 0) ? 0 : _sweepAngle;

            _startAngle = (_startAngle > 360) ? 0 : _startAngle;
            _startAngle = (_startAngle < 0) ? 0 : _startAngle;

            _arcPaint = new Paint();
            _arcPaint.Color = arcColor;
            _arcPaint.AntiAlias = true;
            _arcPaint.SetStyle(Paint.Style.Stroke);
            _arcPaint.StrokeWidth = _arcWidth;
            //mArcPaint.setAlpha(45);

            _progressPaint = new Paint();
            _progressPaint.Color = progressColor;
            _progressPaint.AntiAlias = true;
            _progressPaint.SetStyle(Paint.Style.Stroke);
            _progressPaint.StrokeWidth = _progressWidth;

            if (_hasRoundedEdges)
            {
                _arcPaint.StrokeCap = Paint.Cap.Round;
                _progressPaint.StrokeCap = Paint.Cap.Round;
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (!_clockwise)
            {
                canvas.Scale(-1, 1, _arcRect.CenterX(), _arcRect.CenterY());
            }

            // Draw the arcs
            var arcStart = _startAngle + MAngleOffset + _arcRotation;
            var arcSweep = _sweepAngle;
            canvas.DrawArc(_arcRect, arcStart, arcSweep, false, _arcPaint);
            canvas.DrawArc(_arcRect, arcStart, _progressSweep, false,
                    _progressPaint);

            // Draw the thumb nail
            canvas.Translate(_translateX - _thumbXPos, _translateY - _thumbYPos);
            _thumb.Draw(canvas);
        }

        private static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {

            var height = GetDefaultSize(SuggestedMinimumHeight, heightMeasureSpec);
            var width = GetDefaultSize(SuggestedMinimumWidth, widthMeasureSpec);
            var min = Math.Min(width, height);
            float top = 0;
            float left = 0;
            var arcDiameter = 0;

            _translateX = (int)(width * 0.5f);
            _translateY = (int)(height * 0.5f);

            arcDiameter = min - PaddingLeft;
            _arcRadius = arcDiameter / 2;
            top = height / 2 - (arcDiameter / 2);
            left = width / 2 - (arcDiameter / 2);
            _arcRect.Set(left, top, left + arcDiameter, top + arcDiameter);

            var arcStart = (int)_progressSweep + _startAngle + _arcRotation + 90;
            _thumbXPos = (int)(_arcRadius * Math.Cos(ConvertToRadians(arcStart)));
            _thumbYPos = (int)(_arcRadius * Math.Sin(ConvertToRadians(arcStart)));

            IsTouchInsideEnabled = _isTouchInsideEnabled;
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        }


        public override bool OnTouchEvent(MotionEvent motionEvent)
        {
            switch (motionEvent.Action)
            {
                case MotionEventActions.Down:
                    OnStartTrackingTouch();
                    UpdateOnTouch(motionEvent);
                    break;
                case MotionEventActions.Move:
                    UpdateOnTouch(motionEvent);
                    break;
                case MotionEventActions.Up:
                    OnStopTrackingTouch();
                    Pressed = false;
                    break;
                case MotionEventActions.Cancel:
                    OnStopTrackingTouch();
                    Pressed = false;
                    break;
            }

            return true;
        }


        protected override void DrawableStateChanged()
        {
            base.DrawableStateChanged();
            if (_thumb != null && _thumb.IsStateful)
            {
                var state = GetDrawableState();
                _thumb.SetState(state);
            }
            Invalidate();
        }

        private void OnStartTrackingTouch()
        {
            if (StartTrackingTouch != null)
            {
                StartTrackingTouch(this, new SeekArcTrackingTouchEventArgs(this));
            }
        }

        private void OnStopTrackingTouch()
        {
            if (StopTrackingTouch != null)
            {
                StopTrackingTouch(this, new SeekArcTrackingTouchEventArgs(this));
            }
        }

        private void UpdateOnTouch(MotionEvent motionEvent)
        {
            var ignoreTouch = ShouldIgnoreTouch(motionEvent.GetX(), motionEvent.GetY());
            if (ignoreTouch)
            {
                return;
            }

            Pressed = true;
            _touchAngle = GetTouchDegrees(motionEvent.GetX(), motionEvent.GetY());
            int progress = GetProgressForAngle(_touchAngle);

            OnProgressRefresh(progress, true);
        }

        private bool ShouldIgnoreTouch(float xPos, float yPos)
        {
            var ignore = false;
            float x = xPos - _translateX;
            float y = yPos - _translateY;

            float touchRadius = (float)Math.Sqrt(((x * x) + (y * y)));
            if (touchRadius < _touchIgnoreRadius)
            {
                ignore = true;
            }
            return ignore;
        }

        public static double ConvertToDegrees(double val)
        {

            return val * (180.0 / Math.PI);

        }

        private double GetTouchDegrees(float xPos, float yPos)
        {
            float x = xPos - _translateX;
            float y = yPos - _translateY;
            //invert the x-coord if we are rotating anti-clockwise
            x = (_clockwise) ? x : -x;
            // convert to arc Angle
            double angle = ConvertToDegrees(Math.Atan2(y, x) + (Math.PI / 2)
                    - ConvertToRadians(_arcRotation));
            if (angle < 0)
            {
                angle = 360 + angle;
            }
            angle -= _startAngle;
            return angle;
        }

        private int GetProgressForAngle(double angle)
        {
            var touchProgress = (int)Math.Round(ValuePerDegree() * angle);

            touchProgress = (touchProgress < 0) ? InvalidProgressValue
                    : touchProgress;
            touchProgress = (touchProgress > _max) ? InvalidProgressValue
                    : touchProgress;
            return touchProgress;
        }

        private float ValuePerDegree()
        {
            return (float)_max / _sweepAngle;
        }

        private void OnProgressRefresh(int progress, bool fromUser)
        {
            UpdateProgress(progress, fromUser);
        }

        private void UpdateThumbPosition()
        {
            int thumbAngle = (int)(_startAngle + _progressSweep + _arcRotation + 90);
            _thumbXPos = (int)(_arcRadius * Math.Cos(ConvertToRadians(thumbAngle)));
            _thumbYPos = (int)(_arcRadius * Math.Sin(ConvertToRadians(thumbAngle)));
        }

        private void UpdateProgress(int progress, bool fromUser)
        {

            if (progress == InvalidProgressValue)
            {
                return;
            }

            if (ProgressChanged != null)
            {
                ProgressChanged(this, new SeekArcProgressChangedEventArgs(this, progress, fromUser));
            }

            progress = (progress > _max) ? _max : progress;
            progress = (_progress < 0) ? 0 : progress;

            _progress = progress;
            _progressSweep = (float)progress / _max * _sweepAngle;

            UpdateThumbPosition();

            Invalidate();
        }

        /**
         * Sets a listener to receive notifications of changes to the SeekArc's
         * progress level. Also provides notifications of when the user starts and
         * stops a touch gesture within the SeekArc.
         * 
         * @param l
         *            The seek bar notification listener
         * 
         * @see SeekArc.OnSeekBarChangeListener
         */
        public void SetOnSeekArcChangeListener(IOnSeekArcChangeListener l)
        {
            _onSeekArcChangeListener = l;
        }

    }
}
