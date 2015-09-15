using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Java.Lang;
using Math = System.Math;

namespace DroidSeekArc
{

    /**
     * 
     * SeekArc.java
     * 
     * This is a class that functions much like a SeekBar but
     * follows a circle path instead of a straight line.
     * 
     * @author Neil Davies
     * 
     */
    public class SeekArc : View
    {

        private const string TAG = "SeekArc";
        private static int INVALID_PROGRESS_VALUE = -1;
        // The initial rotational offset -90 means we start at 12 o'clock
        private const int mAngleOffset = -90;

        /**
         * The Drawable for the seek arc thumbnail
         */
        private Drawable mThumb;

        /**
         * The Maximum value that this SeekArc can be set to
         */
        private int mMax = 100;

        /**
         * The Current value that the SeekArc is set to
         */
        private int mProgress = 0;

        /**
         * The width of the progress line for this SeekArc
         */
        private int mProgressWidth = 4;

        /**
         * The Width of the background arc for the SeekArc 
         */
        private int mArcWidth = 2;

        /**
         * The Angle to start drawing this Arc from
         */
        private int mStartAngle = 0;

        /**
         * The Angle through which to draw the arc (Max is 360)
         */
        private int mSweepAngle = 360;

        /**
         * The rotation of the SeekArc- 0 is twelve o'clock
         */
        private int mRotation = 0;

        /**
         * Give the SeekArc rounded edges
         */
        private bool mRoundedEdges = false;

        /**
         * Enable touch inside the SeekArc
         */
        private bool mTouchInside = true;

        /**
         * Will the progress increase clockwise or anti-clockwise
         */
        private bool mClockwise = true;

        // Internal variables
        private int mArcRadius = 0;
        private float mProgressSweep = 0;
        private RectF mArcRect = new RectF();
        private Paint mArcPaint;
        private Paint mProgressPaint;
        private int mTranslateX;
        private int mTranslateY;
        private int mThumbXPos;
        private int mThumbYPos;
        private double mTouchAngle;
        private float mTouchIgnoreRadius;
        private OnSeekArcChangeListener mOnSeekArcChangeListener;

        public interface OnSeekArcChangeListener
        {

            /**
             * Notification that the progress level has changed. Clients can use the
             * fromUser parameter to distinguish user-initiated changes from those
             * that occurred programmatically.
             * 
             * @param seekArc
             *            The SeekArc whose progress has changed
             * @param progress
             *            The current progress level. This will be in the range
             *            0..max where max was set by
             *            {@link ProgressArc#setMax(int)}. (The default value for
             *            max is 100.)
             * @param fromUser
             *            True if the progress change was initiated by the user.
             */
            void OnProgressChanged(SeekArc seekArc, int progress, bool fromUser);

            /**
             * Notification that the user has started a touch gesture. Clients may
             * want to use this to disable advancing the seekbar.
             * 
             * @param seekArc
             *            The SeekArc in which the touch gesture began
             */
            void onStartTrackingTouch(SeekArc seekArc);

            /**
             * Notification that the user has finished a touch gesture. Clients may
             * want to use this to re-enable advancing the seekarc.
             * 
             * @param seekArc
             *            The SeekArc in which the touch gesture began
             */
            void onStopTrackingTouch(SeekArc seekArc);
        }

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
            int thumbHalfheight = 0;
            int thumbHalfWidth = 0;
            mThumb = Resources.GetDrawable(Resource.Drawable.seek_arc_control_selector);
            // Convert progress width to pixels for current density
            mProgressWidth = (int)(mProgressWidth * density);


            if (attrs != null)
            {
                // Attribute initialization
                TypedArray a = context.ObtainStyledAttributes(attrs,
                        Resource.Styleable.SeekArc, defStyle, 0);

                var thumb = a.GetDrawable(Resource.Styleable.SeekArc_thumb);
                if (thumb != null)
                {
                    mThumb = thumb;
                }



                thumbHalfheight = (int)mThumb.IntrinsicHeight / 2;
                thumbHalfWidth = (int)mThumb.IntrinsicWidth / 2;
                mThumb.SetBounds(-thumbHalfWidth, -thumbHalfheight, thumbHalfWidth,
                        thumbHalfheight);

                mMax = a.GetInteger(Resource.Styleable.SeekArc_max, mMax);
                mProgress = a.GetInteger(Resource.Styleable.SeekArc_progress, mProgress);
                mProgressWidth = (int)a.GetDimension(
                        Resource.Styleable.SeekArc_progressWidth, mProgressWidth);
                mArcWidth = (int)a.GetDimension(Resource.Styleable.SeekArc_arcWidth,
                        mArcWidth);
                mStartAngle = a.GetInt(Resource.Styleable.SeekArc_startAngle, mStartAngle);
                mSweepAngle = a.GetInt(Resource.Styleable.SeekArc_sweepAngle, mSweepAngle);
                mRotation = a.GetInt(Resource.Styleable.SeekArc_rotation, mRotation);
                mRoundedEdges = a.GetBoolean(Resource.Styleable.SeekArc_roundEdges,
                        mRoundedEdges);
                mTouchInside = a.GetBoolean(Resource.Styleable.SeekArc_touchInside,
                        mTouchInside);
                mClockwise = a.GetBoolean(Resource.Styleable.SeekArc_clockwise,
                        mClockwise);

                arcColor = a.GetColor(Resource.Styleable.SeekArc_arcColor, arcColor);
                progressColor = a.GetColor(Resource.Styleable.SeekArc_progressColor,
                        progressColor);

                a.Recycle();
            }

            mProgress = (mProgress > mMax) ? mMax : mProgress;
            mProgress = (mProgress < 0) ? 0 : mProgress;

            mSweepAngle = (mSweepAngle > 360) ? 360 : mSweepAngle;
            mSweepAngle = (mSweepAngle < 0) ? 0 : mSweepAngle;

            mStartAngle = (mStartAngle > 360) ? 0 : mStartAngle;
            mStartAngle = (mStartAngle < 0) ? 0 : mStartAngle;

            mArcPaint = new Paint();
            mArcPaint.Color = arcColor;
            mArcPaint.AntiAlias = true;
            mArcPaint.SetStyle(Paint.Style.Stroke);
            mArcPaint.StrokeWidth = mArcWidth;
            //mArcPaint.setAlpha(45);

            mProgressPaint = new Paint();
            mProgressPaint.Color = progressColor;
            mProgressPaint.AntiAlias = true;
            mProgressPaint.SetStyle(Paint.Style.Stroke);
            mProgressPaint.StrokeWidth = mProgressWidth;

            if (mRoundedEdges)
            {
                mArcPaint.StrokeCap = Paint.Cap.Round;
                mProgressPaint.StrokeCap = Paint.Cap.Round;
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (!mClockwise)
            {
                canvas.Scale(-1, 1, mArcRect.CenterX(), mArcRect.CenterY());
            }

            // Draw the arcs
            var arcStart = mStartAngle + mAngleOffset + mRotation;
            var arcSweep = mSweepAngle;
            canvas.DrawArc(mArcRect, arcStart, arcSweep, false, mArcPaint);
            canvas.DrawArc(mArcRect, arcStart, mProgressSweep, false,
                    mProgressPaint);

            // Draw the thumb nail
            canvas.Translate(mTranslateX - mThumbXPos, mTranslateY - mThumbYPos);
            mThumb.Draw(canvas);
        }

        private static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {

            int height = GetDefaultSize(SuggestedMinimumHeight, heightMeasureSpec);
            int width = GetDefaultSize(SuggestedMinimumWidth, widthMeasureSpec);
            int min = Math.Min(width, height);
            float top = 0;
            float left = 0;
            int arcDiameter = 0;

            mTranslateX = (int)(width * 0.5f);
            mTranslateY = (int)(height * 0.5f);

            arcDiameter = min - PaddingLeft;
            mArcRadius = arcDiameter / 2;
            top = height / 2 - (arcDiameter / 2);
            left = width / 2 - (arcDiameter / 2);
            mArcRect.Set(left, top, left + arcDiameter, top + arcDiameter);

            int arcStart = (int)mProgressSweep + mStartAngle + mRotation + 90;
            mThumbXPos = (int)(mArcRadius * Math.Cos(ConvertToRadians(arcStart)));
            mThumbYPos = (int)(mArcRadius * Math.Sin(ConvertToRadians(arcStart)));

            SetTouchInSide(mTouchInside);
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
            if (mThumb != null && mThumb.IsStateful)
            {
                var state = GetDrawableState();
                mThumb.SetState(state);
            }
            Invalidate();
        }

        private void OnStartTrackingTouch()
        {
            if (mOnSeekArcChangeListener != null)
            {
                mOnSeekArcChangeListener.onStartTrackingTouch(this);
            }
        }

        private void OnStopTrackingTouch()
        {
            if (mOnSeekArcChangeListener != null)
            {
                mOnSeekArcChangeListener.onStopTrackingTouch(this);
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
            mTouchAngle = GetTouchDegrees(motionEvent.GetX(), motionEvent.GetY());
            int progress = GetProgressForAngle(mTouchAngle);

            OnProgressRefresh(progress, true);
        }

        private bool ShouldIgnoreTouch(float xPos, float yPos)
        {
            var ignore = false;
            float x = xPos - mTranslateX;
            float y = yPos - mTranslateY;

            float touchRadius = (float)Math.Sqrt(((x * x) + (y * y)));
            if (touchRadius < mTouchIgnoreRadius)
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
            float x = xPos - mTranslateX;
            float y = yPos - mTranslateY;
            //invert the x-coord if we are rotating anti-clockwise
            x = (mClockwise) ? x : -x;
            // convert to arc Angle
            double angle = ConvertToDegrees(Math.Atan2(y, x) + (Math.PI / 2)
                    - ConvertToRadians(mRotation));
            if (angle < 0)
            {
                angle = 360 + angle;
            }
            angle -= mStartAngle;
            return angle;
        }

        private int GetProgressForAngle(double angle)
        {
            var touchProgress = (int)Math.Round(ValuePerDegree() * angle);

            touchProgress = (touchProgress < 0) ? INVALID_PROGRESS_VALUE
                    : touchProgress;
            touchProgress = (touchProgress > mMax) ? INVALID_PROGRESS_VALUE
                    : touchProgress;
            return touchProgress;
        }

        private float ValuePerDegree()
        {
            return (float)mMax / mSweepAngle;
        }

        private void OnProgressRefresh(int progress, bool fromUser)
        {
            UpdateProgress(progress, fromUser);
        }

        private void UpdateThumbPosition()
        {
            int thumbAngle = (int)(mStartAngle + mProgressSweep + mRotation + 90);
            mThumbXPos = (int)(mArcRadius * Math.Cos(ConvertToRadians(thumbAngle)));
            mThumbYPos = (int)(mArcRadius * Math.Sin(ConvertToRadians(thumbAngle)));
        }

        private void UpdateProgress(int progress, bool fromUser)
        {

            if (progress == INVALID_PROGRESS_VALUE)
            {
                return;
            }

            if (mOnSeekArcChangeListener != null)
            {
                mOnSeekArcChangeListener
                        .OnProgressChanged(this, progress, fromUser);
            }

            progress = (progress > mMax) ? mMax : progress;
            progress = (mProgress < 0) ? 0 : progress;

            mProgress = progress;
            mProgressSweep = (float)progress / mMax * mSweepAngle;

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
        public void SetOnSeekArcChangeListener(OnSeekArcChangeListener l)
        {
            mOnSeekArcChangeListener = l;
        }

        public void SetProgress(int progress)
        {
            UpdateProgress(progress, false);
        }

        public int GetProgressWidth()
        {
            return mProgressWidth;
        }

        public void SetProgressWidth(int progressWidth)
        {
            this.mProgressWidth = progressWidth;
            mProgressPaint.StrokeWidth = progressWidth;
        }

        public int GetArcWidth()
        {
            return mArcWidth;
        }

        public void SetArcWidth(int mArcWidth)
        {
            this.mArcWidth = mArcWidth;
            mArcPaint.StrokeWidth = mArcWidth;
        }
        public int GetArcRotation()
        {
            return mRotation;
        }

        public void SetArcRotation(int mRotation)
        {
            this.mRotation = mRotation;
            UpdateThumbPosition();
        }

        public int GetStartAngle()
        {
            return mStartAngle;
        }

        public void SetStartAngle(int mStartAngle)
        {
            this.mStartAngle = mStartAngle;
            UpdateThumbPosition();
        }

        public int GetSweepAngle()
        {
            return mSweepAngle;
        }

        public void SetSweepAngle(int mSweepAngle)
        {
            this.mSweepAngle = mSweepAngle;
            UpdateThumbPosition();
        }

        public void SetRoundedEdges(bool isEnabled)
        {
            mRoundedEdges = isEnabled;
            if (mRoundedEdges)
            {
                mArcPaint.StrokeCap = Paint.Cap.Round;
                mProgressPaint.StrokeCap = Paint.Cap.Round;
            }
            else
            {
                mArcPaint.StrokeCap = Paint.Cap.Square;
                mProgressPaint.StrokeCap = Paint.Cap.Square;
            }
        }

        public void SetTouchInSide(bool isEnabled)
        {
            int thumbHalfheight = (int)mThumb.IntrinsicHeight / 2;
            int thumbHalfWidth = (int)mThumb.IntrinsicWidth / 2;
            mTouchInside = isEnabled;
            if (mTouchInside)
            {
                mTouchIgnoreRadius = (float)mArcRadius / 4;
            }
            else
            {
                // Don't use the exact radius makes interaction too tricky
                mTouchIgnoreRadius = mArcRadius
                        - Math.Min(thumbHalfWidth, thumbHalfheight);
            }
        }

        public void SetClockwise(bool isClockwise)
        {
            mClockwise = isClockwise;
        }
    }

}
