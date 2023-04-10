﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProblemType
{
    ShortAnswer,
    FillVacancy
}

public class Viscera
{
    //脏腑名字
    public string Name;
    //是否在指定位置
    public bool IsGivenPos = false;
    //脏腑物体
    public GameObject VisceraObj;
    //判断点击按钮之后的问题类型
    public ProblemType ProblemType;
    //点击按钮之后的问题
    public string Matter;
    //填空题答案选项
    public List<string> AnswerList = null;
}
