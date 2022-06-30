using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Collections;

namespace Myd.UI
{
    /// <summary>
    /// 模拟CanvasUpdateRegistry
    /// V1、只考虑接受willRenderCanvases
    /// </summary>
    public class MCanvasUpdateRegistry
    {
        private static MCanvasUpdateRegistry s_Instance;
        /// <summary>
        /// Get the singleton registry instance.
        /// </summary>
        public static MCanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new MCanvasUpdateRegistry();
                return s_Instance;
            }
        }

        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        public MCanvasUpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdate;
        }

        private void CleanInvalidItems()
        {
            var graphicRebuildQueueCount = m_GraphicRebuildQueue.Count;
            for (int i = graphicRebuildQueueCount - 1; i >= 0; --i)
            {
                var item = m_GraphicRebuildQueue[i];
                if (item == null)
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }


        private void PerformUpdate() 
        {
            //清理不可用的条目
            CleanInvalidItems();

            for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
            {
                for (var k = 0; k < m_GraphicRebuildQueue.Count; k++)
                {
                    try
                    {
                        var element = m_GraphicRebuildQueue[k];
                        element.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, m_GraphicRebuildQueue[k].transform);
                    }
                }

                m_GraphicRebuildQueue.Clear();
            }
        }

        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        public static bool TryRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_GraphicRebuildQueue == null)
            {
                Debug.LogError(string.Format("Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.", element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            //instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            //if (m_PerformingGraphicUpdate)
            //{
            //    Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
            //    return;
            //}
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.Remove(element);
        }
    }
}
