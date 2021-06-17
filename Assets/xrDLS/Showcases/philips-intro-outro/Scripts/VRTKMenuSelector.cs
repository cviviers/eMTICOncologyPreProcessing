#if VRTK_VERSION_3_1_0_OR_NEWER

using UnityEngine;
using VRTK;
using VRTK.Examples;

// This is the glue code for VRTK to the Philips Branding VRMenu

namespace Philips.Branding
{
    public class VRTKMenuSelector : VRTKExample_PointerObjectHighlighterActivator
    {
        public VRMenu VRMenu;

        public VRTK_ControllerEvents ControllerEvents;

        void Start()
        {
            if (VRMenu == null)
                VRMenu = GetComponent<VRMenu>();
            
            if (VRMenu == null) throw new System.Exception("Philips Branding VRMenu not found!");

            if (ControllerEvents == null)
                ControllerEvents = GetComponent<VRTK_ControllerEvents>();

            if (ControllerEvents == null) throw new System.Exception("VRTK_ControllerEvents not found!");
        }

        bool wasClicking;
        void LateUpdate()
        {
            if (ControllerEvents.IsButtonPressed(VRTK_ControllerEvents.ButtonAlias.ButtonOnePress) ||
                ControllerEvents.IsButtonPressed(VRTK_ControllerEvents.ButtonAlias.ButtonTwoPress) ||
                ControllerEvents.IsButtonPressed(VRTK_ControllerEvents.ButtonAlias.TriggerPress))
            {
                if (wasClicking) return;
                wasClicking = true;
                VRMenu.Click();
            } else wasClicking = false;
        }

        protected override void DestinationMarkerEnter(object sender, DestinationMarkerEventArgs e)
        {
            VRMenu.Select(e.target);
            base.DestinationMarkerEnter(sender, e);
        }

        protected override void DestinationMarkerExit(object sender, DestinationMarkerEventArgs e)
        {
            VRMenu.Select(null);
            base.DestinationMarkerExit(sender, e);
        }
    }
}

#else

namespace Philips.Branding
{
    public class VRTKMenuSelector
    {
        public VRMenu VRMenu;
    }
}

#endif
