using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine //: MonoBehaviour
{
    public PlayerState currentState { get; private set; }
    //初始化函数
    public void Initialize(PlayerState _starState)
    {
        currentState = _starState;
        currentState.Enter();
    }
    //改变状态的函数
    public void ChangeState(PlayerState _newState)
    {
        currentState.Exit();
        currentState = _newState;
        currentState.Enter();
    }



}
