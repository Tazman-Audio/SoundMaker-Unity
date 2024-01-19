using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fabric.Player
{
    public class BasePropertyAttribute : PropertyAttribute
    {
        public string label;
        public string tooltip;
    }

    public class FabricEventAttribute : BasePropertyAttribute { }

    public class ReadOnlyAttribute : PropertyAttribute { }
}