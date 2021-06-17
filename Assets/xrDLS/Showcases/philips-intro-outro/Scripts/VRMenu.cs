using System;
using System.Collections.Generic;
using UnityEngine;

namespace Philips.Branding
{
    public class VRMenu : MonoBehaviour
    {
        [System.Serializable]
        public enum MenuDesign
        {
            CardWithBorder,
            CardWithoutBorder,
            TwoCards,
        }

#region EditorUI

        [Tooltip("Design for the menu items")]
        public MenuDesign Design;

        [Tooltip("Curve the menu items from flat to full curved items")]
        [Range(0f, 2f)]
        public float Curvation = 1f;

        [Tooltip("Maximum number of menu items to show. In case there are more the user can navigate to the extra items.")]
        public int MaximumItems = 4;

        [Tooltip("GameObject to press for one item back in the menu.")]
        public GameObject BackButton;

        [Tooltip("GameObject to press for one item forward in the menu.")]
        public GameObject ForwardButton;

#endregion

        List<VRMenuItem> menuItems = new List<VRMenuItem>();

        VRMenuItem selectedItem;

#region Scrolling items
        bool shouldScroll;
        int scrollIndex = 0;
        bool backSelected, forwardSelected;

        List<Tuple<Vector3, Quaternion>> itemTransforms = new List<Tuple<Vector3, Quaternion>>();
#endregion

        void Start()
        {
            var items = GetComponentsInChildren<VRMenuItem>();
            foreach(var item in items)
                menuItems.Add(item);
            SetItemProperties();
            SetScrollItems();
        }

        private void OnValidate()
        {
            SetItemProperties();
        }

        void SetScrollItems()   // note: do not update in Editor time, else menu items > max can't be edited
        {
            shouldScroll = menuItems.Count > MaximumItems;
            if (shouldScroll)
            {
                itemTransforms.Clear();
                var i = 0;
                foreach(var item in menuItems)
                {
                    var isVisible = i - scrollIndex >= 0 && i - scrollIndex < MaximumItems;
                    item.gameObject.SetActive(isVisible);
                    if (isVisible)
                    {
                        itemTransforms.Add(new Tuple<Vector3, Quaternion>(item.transform.position, item.transform.rotation));
                    }
                    ++i; 
                }
            }
            else
            {
                if (BackButton != null) BackButton.SetActive(false);
                if (ForwardButton != null) ForwardButton.SetActive(false);
            }
        }

        void UpdateScrollItems()   // note: do not update in Editor time, else menu items > max can't be edited
        {
            if (shouldScroll)
            {
                var i = 0;
                var t = 0;
                foreach(var item in menuItems)
                {
                    var isVisible = i - scrollIndex >= 0 && i - scrollIndex < MaximumItems;
                    item.gameObject.SetActive(isVisible);
                    if (isVisible)
                    {
                        var transform = itemTransforms[t];
                        item.transform.position = transform.Item1;
                        item.transform.rotation = transform.Item2;
                        ++t;
                    }
                    ++i; 
                }
            }
        }

        void SetItemProperties()
        {
            var rotateDesign = VRMenuItem.MenuDesign.LeftBottom;
            var items = GetComponentsInChildren<VRMenuItem>();
            foreach(var item in items)
            {
                item.Curvation = Curvation;
                switch(Design)
                {
                    case MenuDesign.CardWithBorder :
                        item.Design = VRMenuItem.MenuDesign.Card;
                        item.ShowBorder = true;
                    break;

                    case MenuDesign.CardWithoutBorder :
                        item.Design = VRMenuItem.MenuDesign.Card;
                        item.ShowBorder = false;
                    break;

                    case MenuDesign.TwoCards :
                        item.Design = rotateDesign;
                    break;
                }
                if (rotateDesign == VRMenuItem.MenuDesign.RightUp)
                    rotateDesign = VRMenuItem.MenuDesign.LeftBottom;
                        else ++rotateDesign;                
            }
        }

        public void Click()
        {
            if (backSelected)
            {
                if (scrollIndex > 0) --scrollIndex;
                UpdateScrollItems();
            }
            if (forwardSelected)
            {
                if (scrollIndex < menuItems.Count - MaximumItems) ++scrollIndex;
                UpdateScrollItems();
            }

            if (selectedItem == null) return;
            selectedItem.Click();
            Select(null);
        }

        public void Select(Transform hit)
        {
            if (hit == null)
                backSelected = forwardSelected = false;
            else if (hit.gameObject == BackButton)
                backSelected = true;
            else if (hit.gameObject == ForwardButton)
                forwardSelected = true;

            var itemName = "";
            if (hit != null)
                itemName = hit.name == "Output" ? hit.parent.name : hit.name;

            if (selectedItem != null) selectedItem.DeSelect();
            selectedItem = null;

            if (string.IsNullOrEmpty(itemName))
            {
                foreach(var item in menuItems)
                    item.Blur(false);
                return;
            }

            var itemFound = false;
            foreach(var item in menuItems)
            {
                if (item.name == itemName)
                {
                    itemFound = true;
                    selectedItem = item;
                    item.Select();
                    item.Blur(false);
                } else item.Blur(true);
            }

            if (!itemFound)
                foreach(var item in menuItems)
                    item.Blur(false);
        }
    }
}
