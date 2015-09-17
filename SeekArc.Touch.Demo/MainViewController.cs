using CoreGraphics;
using UIKit;

namespace SeekArc.Touch.Demo
{
    public class MainViewController : UIViewController
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var circularSlider = new CircularSlider(new CGRect(0, 0, View.Bounds.Width, View.Bounds.Height));
            View.AddSubview(circularSlider);
            View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Top, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Top, 1.0f, 0.0f));
            View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Left, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Left, 1.0f, 0.0f));
            View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Right, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Right, 1.0f, 0.0f));
            View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Bottom, 1.0f, 0.0f));
        }
    }
}