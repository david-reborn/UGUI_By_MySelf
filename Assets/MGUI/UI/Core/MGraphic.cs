

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Myd.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [ExecuteAlways]
    public class MGraphic : UIBehaviour, ICanvasElement
    {
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private Canvas m_Canvas;
        [NonSerialized] private CanvasRenderer m_CanvasRenderer;
        [NonSerialized] protected static Mesh s_Mesh;

        [SerializeField] private Color m_Color = Color.white;
        [SerializeField] protected Material m_Material;

        [NonSerialized] protected bool m_SkipLayoutUpdate; 
        [NonSerialized] protected bool m_SkipMaterialUpdate;

        private bool m_MaterialDirty;
        private bool m_VertsDirty;
        /// <summary>
        /// Mark the Graphic and the canvas as having been changed.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            CacheCanvas();

            if (s_WhiteTexture == null)
                s_WhiteTexture = Texture2D.whiteTexture;
            SetAllDirty();
        }


        protected override void OnDisable()
        {

            MCanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            if (canvasRenderer != null)
                canvasRenderer.Clear();

            base.OnDisable();
        }

        public CanvasRenderer canvasRenderer
        {
            get
            {
                // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_CanvasRenderer, null))
                {
                    m_CanvasRenderer = GetComponent<CanvasRenderer>();

                    if (ReferenceEquals(m_CanvasRenderer, null))
                    {
                        m_CanvasRenderer = gameObject.AddComponent<CanvasRenderer>();
                    }
                }
                return m_CanvasRenderer;
            }
        }
        public RectTransform rectTransform
        {
            get
            {
                // The RectTransform is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_RectTransform, null))
                {
                    m_RectTransform = GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

        protected static Mesh workerMesh
        {
            get
            {
                if (s_Mesh == null)
                {
                    s_Mesh = new Mesh();
                    s_Mesh.name = "Shared UI Mesh";
                    s_Mesh.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Mesh;
            }
        }
        public virtual Color color { get { return m_Color; } 
            set 
            {
                if (Util.SetColor(ref m_Color, value)) 
                {
                    Debug.Log("SetColor");
                    SetVerticesDirty();
                } 
            } 
        }
        static protected Texture2D s_WhiteTexture = null;
        public virtual Texture mainTexture
        {
            get
            {
                return s_WhiteTexture;
            }
        }
        private void CacheCanvas()
        {
            var list = ListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count > 0)
            {
                // Find the first active and enabled canvas.
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].isActiveAndEnabled)
                    {
                        m_Canvas = list[i];
                        break;
                    }

                    // if we reached the end and couldn't find an active and enabled canvas, we should return null . case 1171433
                    if (i == list.Count - 1)
                        m_Canvas = null;
                }
            }
            else
            {
                m_Canvas = null;
            }

            ListPool<Canvas>.Release(list);
        }

        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    CacheCanvas();
                return m_Canvas;
            }
        }

        public virtual void SetAllDirty()
        {
            //SetLayoutDirty();
            SetMaterialDirty();
            SetVerticesDirty();
        }

        public virtual void SetVerticesDirty()
        {
            if (!IsActive())
                return;

            m_VertsDirty = true;
            MCanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        public virtual void SetMaterialDirty()
        {
            if (!IsActive())
                return;

            m_MaterialDirty = true;
            MCanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.PreRender:
                {
                    if (m_VertsDirty)
                    {
                        UpdateGeometry();
                        m_VertsDirty = false;
                    }
                    if (m_MaterialDirty)
                    {
                        UpdateMaterial();
                        m_MaterialDirty = false;
                    }
                    break;
                }
            }
        }

        protected virtual void UpdateGeometry()
        {
            DoMeshGeneration();
        }

        private void DoMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
                OnPopulateMesh(s_VertexHelper);
            else
                s_VertexHelper.Clear(); // clear the vertex helper so invalid graphics dont draw.

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);

            ListPool<Component>.Release(components);

            s_VertexHelper.FillMesh(workerMesh);
            canvasRenderer.SetMesh(workerMesh);
        }

        protected virtual void OnPopulateMesh(Mesh m)
        {
            OnPopulateMesh(s_VertexHelper);
            s_VertexHelper.FillMesh(m);
        }

        protected virtual void OnPopulateMesh(VertexHelper vh)
        {
            var r = rectTransform.rect;
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);

            Color32 color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
        static protected Material s_DefaultUI = null;
        static public Material defaultGraphicMaterial
        {
            get
            {
                if (s_DefaultUI == null)
                    s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
                return s_DefaultUI;
            }
        }
        public virtual Material defaultMaterial
        {
            get { return defaultGraphicMaterial; }
        }
        public virtual Material material
        {
            get
            {
                return (m_Material != null) ? m_Material : defaultMaterial;
            }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                SetMaterialDirty();
            }
        }
        public virtual Material materialForRendering
        {
            get
            {
                var components = ListPool<Component>.Get();
                GetComponents(typeof(IMaterialModifier), components);

                var currentMat = material;
                for (var i = 0; i < components.Count; i++)
                    currentMat = (components[i] as IMaterialModifier).GetModifiedMaterial(currentMat);
                ListPool<Component>.Release(components);
                return currentMat;
            }
        }
        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {

        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetAllDirty();
        }
#endif
    }

}
