using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelWorldInputs : MonoBehaviour
{
    [SerializeField] private float damagePerSecond;
    public bool IsGamePaused;

    private UIController _uiController;
    private World _world;

    private void Start()
    {
        _uiController = FindObjectOfType<UIController>();
        _world = FindObjectOfType<World>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            IsGamePaused = !IsGamePaused;
            _uiController.OpenMenu(IsGamePaused);
        }
        
        if(IsGamePaused)
            return;
        
        if(Input.GetMouseButton(0))
            _world.Dig(damagePerSecond * Time.deltaTime);
        else if(Input.GetMouseButtonUp(0))
            _world.ResetLastDigBlockHealth();
        else if(Input.GetMouseButtonDown(1))
            _world.Build();
        
        if(Input.GetKeyDown(KeyCode.F5))
            WorldSaver.Save(_world);
        
        if(Input.GetKeyDown(KeyCode.Alpha1))
            _uiController.SelectBlock(1);
        else if(Input.GetKeyDown(KeyCode.Alpha2))
            _uiController.SelectBlock(2);
        else if(Input.GetKeyDown(KeyCode.Alpha3))
            _uiController.SelectBlock(3);
        else if(Input.GetKeyDown(KeyCode.Alpha4))
            _uiController.SelectBlock(4);
        else if(Input.GetKeyDown(KeyCode.Alpha5))
            _uiController.SelectBlock(5);
        else if(Input.GetKeyDown(KeyCode.Alpha6))
            _uiController.SelectBlock(6);
        else if(Input.GetKeyDown(KeyCode.Alpha7))
            _uiController.SelectBlock(7);
        else if(Input.GetKeyDown(KeyCode.Alpha8))
            _uiController.SelectBlock(8);
        else if(Input.GetKeyDown(KeyCode.Alpha9))
            _uiController.SelectBlock(9);
    }
}
