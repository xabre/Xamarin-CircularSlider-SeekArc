using CoreGraphics;
using UIKit;
using Foundation;
using System.Collections.Generic;

namespace SeekArc.Touch.Demo
{
    public class MainViewController : UIViewController
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

			View.BackgroundColor = UIColor.LightGray;
	

			var circularSlider = new CircularSlider(new CGRect(0, 100, View.Bounds.Width, View.Bounds.Width));
			circularSlider.SetHandleType (CircularSlider.CircularSliderHandleType.CircularSliderHandleTypeDoubleCircleWithOpenCenter);
			circularSlider.BackgroundColor = UIColor.FromWhiteAlpha (0, 0.1f);		
				circularSlider.SetFilledColor (UIColor.FromRGBA (155/255f, 211/255f, 156/255f, 1.0f));

			var nsarray = new List<string> (){ new NSString ("1"), new NSString ("2"),new NSString("3"),new NSString("4"),new NSString("5"), new NSString("6") };

			circularSlider.SetInnerMarkingLabels (nsarray);
            View.AddSubview(circularSlider);
            //View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Top, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Top, 1.0f, 0.0f));
            //View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Left, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Left, 1.0f, 0.0f));
            //View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Right, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Right, 1.0f, 0.0f));
            //View.AddConstraint(NSLayoutConstraint.Create(View, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, circularSlider, NSLayoutAttribute.Bottom, 1.0f, 0.0f));
        }
    }
}