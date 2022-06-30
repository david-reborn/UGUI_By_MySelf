using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Myd.UI
{
    [AddComponentMenu("Myd/UI/Image", 1)]
    public class MImage : MGraphic, IClippable, IMaskable, IMaterialModifier
    {
        [FormerlySerializedAs("m_Frame")]
        [SerializeField]
        private Sprite m_Sprite;

        private bool m_Tracked = false;

        static bool s_Initialized;
        static List<MImage> m_TrackedTexturelessImages = new List<MImage>();
        public Sprite sprite
        {
            get { return m_Sprite; }
            set
            {
                if (m_Sprite != null)
                {
                    if (m_Sprite != value)
                    {
                        m_SkipLayoutUpdate = m_Sprite.rect.size.Equals(value ? value.rect.size : Vector2.zero);
                        m_SkipMaterialUpdate = m_Sprite.texture == (value ? value.texture : null);
                        m_Sprite = value;

                        SetAllDirty();
                        TrackSprite();
                    }
                }
                else if (value != null)
                {
                    m_SkipLayoutUpdate = value.rect.size == Vector2.zero;
                    m_SkipMaterialUpdate = value.texture == null;
                    m_Sprite = value;

                    SetAllDirty();
                    TrackSprite();
                }
            }
        }

        private Sprite activeSprite { get { return sprite; } }

        protected MImage()
        {
        }

        /// <summary>
        /// Image's texture comes from the UnityEngine.Image.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (activeSprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return activeSprite.texture;
            }
        }

        private void TrackSprite()
        {
            if (activeSprite != null && activeSprite.texture == null)
            {
                Debug.LogError("Image--TrackSprite");
                TrackImage(this);
                m_Tracked = true;
            }
        }
        private static void TrackImage(MImage g)
        {
            if (!s_Initialized)
            {
                SpriteAtlasManager.atlasRegistered += RebuildImage;
                s_Initialized = true;
            }

            m_TrackedTexturelessImages.Add(g);
        }

        static void RebuildImage(SpriteAtlas spriteAtlas)
        {
            for (var i = m_TrackedTexturelessImages.Count - 1; i >= 0; i--)
            {
                var g = m_TrackedTexturelessImages[i];
                if (null != g.activeSprite && spriteAtlas.CanBindTo(g.activeSprite))
                {
                    g.SetAllDirty();
                    m_TrackedTexturelessImages.RemoveAt(i);
                }
            }
        }

        //绘制Mesh
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (activeSprite == null)
            {
                base.OnPopulateMesh(toFill);
                return;
            }
            //Simple类型的Sprite
            GenerateSimpleSprite(toFill, false);
        }

        void GenerateSimpleSprite(VertexHelper vh, bool lPreserveAspect)
        {
            //计算Sprite的实际大小，返回一个矩形
            Vector4 v = GetDrawingDimensions(lPreserveAspect);
            //Sprite在所要渲染的Texture上的UV坐标，会扣减Padding的边框值，采用实际Sprite的最小AABB
            var uv = (activeSprite != null) ? UnityEngine.Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;

            var color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
        private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
        {
            //获取当前Sprite的Padding属性，Padding其实应该是图片存在像素的AABB包围盒
            var padding = activeSprite == null ? Vector4.zero : UnityEngine.Sprites.DataUtility.GetPadding(activeSprite);
            var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

            //考虑PixelPrefect的情况
            Rect r = rectTransform.rect;
            // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);

            var v = Vector4.zero;
            v.x = padding.x / spriteW;
            v.y = padding.y / spriteH;
            v.z = (spriteW - padding.z) / spriteW;
            v.w = (spriteH - padding.w) / spriteH;

            //if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
            //{
            //    PreserveSpriteAspectRatio(ref r, size);
            //}

            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
            );

            return v;
        }

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();

            // check if this sprite has an associated alpha texture (generated when splitting RGBA = RGB + A as two textures without alpha)

            if (activeSprite == null)
            {
                canvasRenderer.SetAlphaTexture(null);
                return;
            }

            Texture2D alphaTex = activeSprite.associatedAlphaSplitTexture;

            if (alphaTex != null)
            {
                canvasRenderer.SetAlphaTexture(alphaTex);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSprite();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_Tracked)
                UnTrackImage(this);
        }

        private static void UnTrackImage(MImage g)
        {
            m_TrackedTexturelessImages.Remove(g);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) 
            {
                var padding = activeSprite == null ? Vector4.zero : UnityEngine.Sprites.DataUtility.GetPadding(activeSprite);
                Debug.Log("Padding:" + padding);
            }
        }

        readonly Vector3[] m_Corners = new Vector3[4];

        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);

                if (canvas)
                {
                    Matrix4x4 mat = canvas.rootCanvas.transform.worldToLocalMatrix;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = mat.MultiplyPoint(m_Corners[i]);
                }

                // bounding box is now based on the min and max of all corners (case 1013182)

                Vector2 min = m_Corners[0];
                Vector2 max = m_Corners[0];
                for (int i = 1; i < 4; i++)
                {
                    min.x = Mathf.Min(m_Corners[i].x, min.x);
                    min.y = Mathf.Min(m_Corners[i].y, min.y);
                    max.x = Mathf.Max(m_Corners[i].x, max.x);
                    max.y = Mathf.Max(m_Corners[i].y, max.y);
                }

                return new Rect(min, max - min);
            }
        }

        private RectMask2D m_ParentMask;
        [SerializeField]
        private bool m_Maskable = true;
        public bool maskable
        {
            get { return m_Maskable; }
            set
            {
                if (value == m_Maskable)
                    return;
                m_Maskable = value;
                m_ShouldRecalculateStencil = true;
                SetMaterialDirty();
            }
        }
        private void UpdateClipParent()
        {
            var newParent = (maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;

            // if the new parent is different OR is now inactive
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.IsActive()))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }

            // don't re-add it if the newparent is inactive
            if (newParent != null && newParent.IsActive())
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }

        public void RecalculateClipping()
        {
            UpdateClipParent();
        }

        public void Cull(Rect clipRect, bool validRect)
        {
            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            UpdateCull(cull);
        }

        private void UpdateCull(bool cull)
        {
            if (canvasRenderer.cull != cull)
            {
                canvasRenderer.cull = cull;
                //UISystemProfilerApi.AddMarker("MaskableGraphic.cullingChanged", this);
                //m_OnCullStateChanged.Invoke(cull);
                //OnCullingChanged();
            }
        }

        public void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
                canvasRenderer.EnableRectClipping(clipRect);
            else
                canvasRenderer.DisableRectClipping();
        }

        public void SetClipSoftness(Vector2 clipSoftness)
        {
            canvasRenderer.clippingSoftness = clipSoftness;
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                if (maskable)
                {
                    var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                    m_StencilValue = MaskUtilities.GetStencilDepth(transform, rootCanvas);
                }
                else
                    m_StencilValue = 0;

                m_ShouldRecalculateStencil = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            if (m_StencilValue > 0 && !isMaskingGraphic)
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }
            return toUse;
        }
        public bool isMaskingGraphic
        {
            get { return false; }
            set
            {
                if (value == m_IsMaskingGraphic)
                    return;

                m_IsMaskingGraphic = value;
            }
        }
        private bool m_IsMaskingGraphic = false;
        [NonSerialized]
        protected int m_StencilValue;
        [NonSerialized]
        protected bool m_ShouldRecalculateStencil = true;
        [NonSerialized]
        protected Material m_MaskMaterial;
        public virtual void RecalculateMasking()
        {
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }
        

#endif
    }
}
