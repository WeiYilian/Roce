﻿using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PuzzleType
{
    OneJigsawPuzzle,
    TwoJigsawPuzzle
}

public enum PuzzleState
{
    Small,
    Big,
    NotVariable
}

public class Puzzle : MonoBehaviour, IPointerClickHandler
{
    private RectTransform m_rectTransform;

    private CanvasGroup maskCanGro;
    
    // 判断是否可以进行大拼图切换
    public bool exchangePuzzle;
    // 拼图需要达到的目标位置索引
    public int jpTargetIndex;
    // 判断是否可以拼图尺寸是否可变化
    public PuzzleState PuzzleState;
    // 拼图的类型
    public PuzzleType PuzzleType;
    // 当前位置编号
    public int jpCurIndex;
    //遮罩列表
    public List<Image> Maskes;
    // 判断是否拼图成功
    private bool isSucceed;
    // 上一次点击时间
    private float lastClickTime;
    // 两次点击之间的最大时间间隔
    private float clickInterval;
    
    public bool flashing;
    //记录原来信息，方便回退
    [HideInInspector] public Vector3 originalPos;
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public GameObject ParentObj;

    private List<Transform> TraList = new List<Transform>();
    
    // OneFaceManager
    private OneFaceManager oneFaceManager;

    private void Awake()
    {
        m_rectTransform = transform.GetComponent<RectTransform>();
    }

    private void Start()
    {
        Init();
    }

    // 初始化
    private void Init()
    {
        // 获取OneFaceManager
        oneFaceManager = UIFaceManager.Instance.GetOneFaceManager();
        // 将自身加进字典中
        oneFaceManager.PuzzleDic[jpCurIndex] = this;
        // 初始化时间间隔
        clickInterval = 0.3f;
    }

    private bool isDisPlay;
    private void Update()
    {
        if(flashing)
        {
            if (!isDisPlay)
            {
                maskCanGro.alpha = Mathf.Lerp(maskCanGro.alpha, 1, 0.02f);
                if(maskCanGro.alpha >= 0.9f)
                    isDisPlay = !isDisPlay;
            }
                
            if (isDisPlay)
            {
                maskCanGro.alpha = Mathf.Lerp(maskCanGro.alpha, 0, 0.02f);
                if(maskCanGro.alpha <= 0.1f)
                    isDisPlay = !isDisPlay;
            }
                
        }
    }


    /// <summary>
    /// 大拼图切换
    /// </summary>
    /// <param name="eventData"></param>
    private void ExchangeBigPuzzle(PointerEventData eventData)
    {
        if (!exchangePuzzle) return;
        
        if (!oneFaceManager.InitialObj)
        {
            // 如果未进行第一次点击，就记录第一次点击的物体
            oneFaceManager.InitialObj = eventData.pointerCurrentRaycast.gameObject;
            // 同时使第一个点击的物体触发点击特效
            maskCanGro = Maskes[jpCurIndex - 1].GetComponent<CanvasGroup>();
            flashing = true;
        }
        else
        {
            // 如果是双击则不触发交换位置方法和闪烁方法
            Puzzle puzzle = oneFaceManager.InitialObj.GetComponent<Puzzle>();
            if (Time.time - lastClickTime < clickInterval)
            {
                //停止闪烁
                puzzle.flashing = false;
                flashing = false;
                Maskes[puzzle.jpCurIndex - 1].GetComponent<CanvasGroup>().alpha = 0;
                return;
            }
            
            // 存放第一次点击的物体位置
            Vector3 firstPos = oneFaceManager.InitialObj.transform.position;
            // 存放第二次点击的物体位置
            Vector3 secondPos = eventData.pointerCurrentRaycast.gameObject.transform.position;
            // 两个物体之间的位置切换
            oneFaceManager.InitialObj.transform.position = secondPos;
            secondPos = firstPos;
            eventData.pointerCurrentRaycast.gameObject.transform.position = secondPos;
            //停止闪烁
            puzzle.flashing = false;
            Maskes[puzzle.jpCurIndex - 1].GetComponent<CanvasGroup>().alpha = 0;
            // 对Puzzle的属性进行交换
            oneFaceManager.ExchangePuzzleCharacteristic(eventData.pointerCurrentRaycast.gameObject.GetComponent<Puzzle>());
            // 切换完成，释放第一次点击后存储的对象
            oneFaceManager.InitialObj = null;
        }
    }

    
    /// <summary>
    /// 大小拼图切换
    /// </summary>
    private void ExchangeSmallPuzzle()
    {
        if (PuzzleState == PuzzleState.NotVariable) return;
        // 判断两次点击时间是否超过clickInterval
        if (Time.time - lastClickTime < clickInterval)
        {
            switch (PuzzleState)
            {
                case PuzzleState.Small:
                    VariableBig();
                    break;
                case PuzzleState.Big:
                    VariableSmall();
                    break;
                case PuzzleState.NotVariable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            // 更新上一次点击的时间,记录点击时间
            lastClickTime = Time.time;
        }
    }

    /// <summary>
    /// 放大的方法
    /// </summary>
    private void VariableBig()
    {
        // 记录父物体(大拼图)
        ParentObj = transform.parent.gameObject;
        // 记录初始信息
        originalPos = transform.localPosition;
        originalSize = m_rectTransform.sizeDelta;
        // 将小拼图的相对位置设置在中心
        transform.localPosition = Vector3.zero;
        // 将小拼图变大
        m_rectTransform.sizeDelta = ParentObj.GetComponent<RectTransform>().rect.size;
        // 隐藏父物体
        ParentObj.SetActive(false);
        if (TraList.Count > 0)
        {
            // 遍历子物体，将子物体显示
            foreach (Transform child in TraList)
            {
                child.gameObject.SetActive(true);
            }
            // 清空TraList
            TraList.Clear();
        }
        // 改变父物体
        transform.SetParent(ParentObj.transform.parent);
        // 对Puzzle的属性进行交换
        oneFaceManager.VariablePuzzleCharacteristic(this);
        // 放大之后调整设置
        SwitchState();
    }

    /// <summary>
    /// 缩小的方法
    /// </summary>
    private void VariableSmall()
    {
        // 显示父物体
        ParentObj.SetActive(true);
        // 设置父物体
        transform.SetParent(ParentObj.transform);
        // 遍历子物体，并将子物体隐藏
        foreach (Transform child in transform)
        {
            TraList.Add(child);
            child.gameObject.SetActive(false);
        }
        // 设置回原来的参数
        transform.localPosition = originalPos;
        m_rectTransform.sizeDelta = originalSize;
        // 对Puzzle的属性进行交换
        oneFaceManager.VariablePuzzleCharacteristic(this);
        // 缩小之后调整设置
        SwitchState();
    }
    
    
    // 点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        // 拼图位置切换
        ExchangeBigPuzzle(eventData);
        // 拼图大小切换
        ExchangeSmallPuzzle();
    }

    
    /// <summary>
    /// 拼图状态切换
    /// </summary>
    public void SwitchState()
    {
        switch (PuzzleState)
        {
            case PuzzleState.Small:
                PuzzleState = PuzzleState.Big;
                exchangePuzzle = true;
                break;
            case PuzzleState.Big:
                PuzzleState = PuzzleState.Small;
                exchangePuzzle = false;
                break;
            case PuzzleState.NotVariable:
                exchangePuzzle = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
