using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Dropdown : MonoBehaviour
{
    [SerializeField] 
    public TMP_Dropdown dropdown;

    public void FixedUpdate()
    {
        if (dropdown.value == 0)
        {
            Debug.Log("1");
        }
    }
    public void DropdownSample(int index)
    {
        switch(index)
        {
            case 0: Debug.Log("1"); break;
            case 1: Debug.Log("2"); break;
            case 2: Debug.Log("3"); break;
        }
        
        
    }

}
