using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meter : MonoBehaviour
{
    [SerializeField] private GameObject m_Mask;
    private RectTransform m_RectTransform;
    private float m_OriginalBarSize;

    private void Awake()
    {
        m_RectTransform = m_Mask.GetComponent<RectTransform>();
        m_OriginalBarSize = m_RectTransform.sizeDelta.x;
    }

    public void UpdateMeter(float progress)
    {
        m_RectTransform.sizeDelta = new Vector2(m_OriginalBarSize * progress, m_RectTransform.sizeDelta.y);
    }
}
