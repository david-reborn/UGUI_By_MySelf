

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Collections;

namespace Myd.UI
{
    public class MGraphicRegistry
    {
        private static MGraphicRegistry s_Instance;
        /// <summary>
        /// The singleton instance of the GraphicRegistry. Creates a new instance if it does not exist.
        /// </summary>
        public static MGraphicRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new MGraphicRegistry();
                return s_Instance;
            }
        }

        private readonly Dictionary<Canvas, IndexedSet<MGraphic>> m_Graphics = new Dictionary<Canvas, IndexedSet<MGraphic>>();

        protected MGraphicRegistry()
        {

        }
    }

}
