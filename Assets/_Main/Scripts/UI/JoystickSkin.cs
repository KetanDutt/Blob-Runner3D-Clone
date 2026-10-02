using UnityEngine;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>
    /// The floating joystick of the scene is an invisible touch area (its images are disabled). This gives it a subtle
    /// visual - a translucent ring where the finger went down and a knob that follows it - so players see that their
    /// input is registered. Purely cosmetic; the input logic of the joystick is untouched.
    /// </summary>
    public static class JoystickSkin
    {
        public static void Apply()
        {
            var joystick = Object.FindObjectOfType<FloatingJoystick>(true);
            if (joystick == null)
                return;

            var background = joystick.transform.Find("Background");
            var handle = background != null ? background.Find("Handle") : null;

            Style(background, 0.18f);
            Style(handle, 0.6f);
        }

        private static void Style(Transform target, float alpha)
        {
            if (target == null)
                return;

            var image = target.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = UISprites.Circle;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false; // the invisible root of the joystick receives the touches
            image.enabled = true;
        }
    }
}
