using System;
using System.Collections.Generic;
using UnityEngine;


public class Voxon_Movement : MonoBehaviour {

    float epsilon = 0.25f;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        
        Debug.Log("x: " + Voxon.Input.GetAxis("Horizontal") + " y: " + Voxon.Input.GetAxis("Vertical"));

        if (Math.Abs(Voxon.Input.GetAxis("Horizontal")) > epsilon)
        {    
            transform.Translate(-1 * (Voxon.Input.GetAxis("Horizontal")) * transform.forward * Time.deltaTime, Space.World);
        }
        if (Math.Abs(Voxon.Input.GetAxis("Vertical")) > epsilon)
        {   
            transform.Translate((Voxon.Input.GetAxis("Vertical")) * transform.right * Time.deltaTime, Space.World);
        }

        if (Voxon.Input.GetKey("Forward") || Voxon.Input.GetButton("Forward"))
        {
            transform.Translate(-1 * transform.forward * Time.deltaTime, Space.World);
        }
        if (Voxon.Input.GetKey("Backward") || Voxon.Input.GetButton("Backward"))
        {
            transform.Translate(transform.forward * Time.deltaTime, Space.World);
        }
        if (Voxon.Input.GetKey("Left") || Voxon.Input.GetButton("Left"))
        {
            transform.Translate(-1 * transform.right * Time.deltaTime, Space.World);
        }
        if (Voxon.Input.GetKey("Right") || Voxon.Input.GetButton("Right"))
        {
            transform.Translate(transform.right * Time.deltaTime, Space.World);
        }

        // Reminder; If you intend to have a quit key; remove the default from VoxieCaptureVolume.cs
        if (Voxon.Input.GetKey("Quit") || Voxon.Input.GetButton("Quit"))
        {
            Application.Quit();
        }

    }
}
