using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.ScrollRect;

namespace Myd.UI
{
    /// <summary>
    /// 
    /// </summary>
    [AddComponentMenu("Myd/UI/Scroll Rect", 37)]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MScrollRect : UIBehaviour, ICanvasElement, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }
        [System.NonSerialized] private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        [Serializable]
        /// <summary>
        /// Event type used by the ScrollRect.
        /// </summary>
        public class ScrollRectEvent : UnityEvent<Vector2> { }
        [SerializeField]
        private RectTransform m_Content;
        [SerializeField]
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();
        [SerializeField]
        private RectTransform m_Viewport;
        private RectTransform m_ViewRect;

        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = m_Viewport;
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }
        private bool m_Dragging;
        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        [SerializeField]
        private bool m_Horizontal = true;
        public bool horizontal { get { return m_Horizontal; } set { m_Horizontal = value; } }

        [SerializeField]
        private bool m_Vertical = true;
        public bool vertical { get { return m_Vertical; } set { m_Vertical = value; } }

        protected MScrollRect()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();

            //if (m_HorizontalScrollbar)
            //    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            //if (m_VerticalScrollbar)
            //    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            Debug.Log("Rebuild Start");
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                //UpdateBounds();
                //UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                //m_HasRebuiltLayout = true;
            }
            Debug.Log("Rebuild Overd");
        }

        void UpdateCachedData()
        {
            Debug.Log("UpdateCachedData");
        }

        protected void UpdatePrevData()
        {
            Debug.Log("UpdatePrevData");
        }

        protected void SetDirty()
        {
            Debug.Log("SetDirty");
            if (!IsActive())
                return;

            //调整当前组件在父节点中的Layout
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void LayoutComplete()
        {
            Debug.Log("LayoutComplete");
        }

        public void GraphicUpdateComplete()
        {
            throw new System.NotImplementedException();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetDirty();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;
            m_Dragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;
            if (m_MovementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        protected Bounds m_ContentBounds;
        private Bounds m_ViewBounds;

        private readonly Vector3[] m_Corners = new Vector3[4];
        private Bounds GetBounds()
        {
            if (m_Content == null)
                return new Bounds();
            m_Content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        //计算Bounds的大小
        protected void UpdateBounds()
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            var contentPivot = m_Content.pivot;
            //调整Bounds
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            if (movementType == MovementType.Clamped)
            {
                // Adjust content so that content bounds bottom (right side) is never higher (to the left) than the view bounds bottom (right side).
                // top (left side) is never lower (to the right) than the view bounds top (left side).
                // All this can happen if content has shrunk.
                // This works because content size is at least as big as view size (because of the call to InternalUpdateBounds above).
                Vector2 delta = Vector2.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x)
                {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
                {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }

                if (m_ViewBounds.min.y < m_ContentBounds.min.y)
                {
                    delta.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                else if (m_ViewBounds.max.y > m_ContentBounds.max.y)
                {
                    delta.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = m_Content.anchoredPosition + delta;
                    if (!m_Horizontal)
                        contentPos.x = m_Content.anchoredPosition.x;
                    if (!m_Vertical)
                        contentPos.y = m_Content.anchoredPosition.y;
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        //调整Bounds的大小，确保ContentSize大于View
        internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
        {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ref m_ViewBounds, ref m_ContentBounds, m_Horizontal, m_Vertical, m_MovementType, ref delta);
        }

        internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, MovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        /// <summary>
        /// Sets the anchored position of the content.
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (!m_Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (!m_Vertical)
                position.y = m_Content.anchoredPosition.y;

            if (position != m_Content.anchoredPosition)
            {
                m_Content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        //private void EnsureLayoutHasRebuilt()
        //{
        //    if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
        //        Canvas.ForceUpdateCanvases();
        //}
        protected virtual void LateUpdate()
        {
            if (!m_Content)
                return;

            //EnsureLayoutHasRebuilt();
            UpdateBounds();
            //float deltaTime = Time.unscaledDeltaTime;
            //Vector2 offset = CalculateOffset(Vector2.zero);
            //if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
            //{
            //    Vector2 position = m_Content.anchoredPosition;
            //    for (int axis = 0; axis < 2; axis++)
            //    {
            //        // Apply spring physics if movement is elastic and content has an offset from the view.
            //        if (m_MovementType == MovementType.Elastic && offset[axis] != 0)
            //        {
            //            float speed = m_Velocity[axis];
            //            float smoothTime = m_Elasticity;
            //            if (m_Scrolling)
            //                smoothTime *= 3.0f;
            //            position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
            //            if (Mathf.Abs(speed) < 1)
            //                speed = 0;
            //            m_Velocity[axis] = speed;
            //        }
            //        // Else move content according to velocity with deceleration applied.
            //        else if (m_Inertia)
            //        {
            //            m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
            //            if (Mathf.Abs(m_Velocity[axis]) < 1)
            //                m_Velocity[axis] = 0;
            //            position[axis] += m_Velocity[axis] * deltaTime;
            //        }
            //        // If we have neither elaticity or friction, there shouldn't be any velocity.
            //        else
            //        {
            //            m_Velocity[axis] = 0;
            //        }
            //    }

            //    if (m_MovementType == MovementType.Clamped)
            //    {
            //        offset = CalculateOffset(position - m_Content.anchoredPosition);
            //        position += offset;
            //    }

            //    SetContentAnchoredPosition(position);
            //}

            //if (m_Dragging && m_Inertia)
            //{
            //    Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
            //    m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            //}

            //if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            //{
            //    UpdateScrollbars(offset);
            //    UISystemProfilerApi.AddMarker("ScrollRect.value", this);
            //    m_OnValueChanged.Invoke(normalizedPosition);
            //    UpdatePrevData();
            //}
            //UpdateScrollbarVisibility();
            //m_Scrolling = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            Debug.Log(eventData);
        }
    }
}
