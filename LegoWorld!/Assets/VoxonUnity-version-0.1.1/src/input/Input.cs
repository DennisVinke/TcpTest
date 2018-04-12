using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace Voxon
{
    /// <summary>  
    ///  Voxon.Input is a Unity input replacement. Utilises Keybindings as set in Capture Volume
    ///  </summary>
    ///  <remarks>
    ///  Unity.Input does not allow input via -Batchmode (required for VX1), thus requiring the use of Voxon.Input
    ///  For single player simply replace Input with Voxon.Input and ensure binding strings are available in Input Controller (found on Capture Volume)
    ///  For multiplayer games; use GetXY(BindingName, PlayerNumber). Players are numbered 0-3.
    /// </remarks>  
    public class Input : VoxieHelper
    {
        // Keyboard Input
        public static bool GetKey(string key_name)
        {
            int key = (int)InputController.Instance.GetKey(key_name);
            int ks = voxie_keystat(key);
            return (ks == 1 || ks == 3);
        }

        public static bool GetKeyUp(string key_name)
        {
            return (voxie_keystat((int)InputController.Instance.GetKey(key_name)) == 0);
        }

        public static bool GetKeyDown(string key_name)
        {
            return (voxie_keystat((int)InputController.Instance.GetKey(key_name)) == 1);
        }

        // Player 1 Default Input
        public static bool GetButton(string button_name)
        {
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            short B = (short)InputController.Instance.GetButton(button_name, 1);
            int r = vcv.GetButtons(0) & B;
            return (r > 0);
        }

        public static bool GetButtonDown(string button_name)
        {
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            short B = (short)InputController.Instance.GetButton(button_name, 1);
            int button_state = vcv.GetButtons(0) & B;
            int old_button_state = vcv.GetOldButtons(0) & B;
            return (old_button_state == 0 & button_state > 0);
        }

        public static bool GetButtonUp(string button_name)
        {
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            short B = (short)InputController.Instance.GetButton(button_name, 1);
            int button_state = vcv.GetButtons(0) & B;
            int old_button_state = vcv.GetOldButtons(0) & B;
            return (old_button_state > 0 & button_state == 0);
        }

        public static float GetAxis(string axis_name)
        {
            int A = (int)InputController.Instance.GetAxis(axis_name, 1);
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            return vcv.GetAxis(A, 0);
        }

        // Multiplayer Input
        public static bool GetButton(string button_name, int player)
        {
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            short B = (short)InputController.Instance.GetButton(button_name, 1);
            int r = vcv.GetButtons(player) & B;
            return (r > 0);
        }

        public static bool GetButtonDown(string button_name, int player)
        {
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            short B = (short)InputController.Instance.GetButton(button_name, 1);
            int button_state = vcv.GetButtons(player) & B;
            int old_button_state = vcv.GetOldButtons(player) & B;
            return (old_button_state == 0 & button_state > 0);
        }

        public static bool GetButtonUp(string button_name, int player)
        {
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            short B = (short)InputController.Instance.GetButton(button_name, 1);
            int button_state = vcv.GetButtons(player) & B;
            int old_button_state = vcv.GetOldButtons(player) & B;
            return (old_button_state > 0 & button_state == 0);
        }

        public static float GetAxis(string axis_name, int player)
        {
            int A = (int)InputController.Instance.GetAxis(axis_name, 1);
            VoxieCaptureVolume vcv = FindObjectOfType(typeof(VoxieCaptureVolume)) as VoxieCaptureVolume;
            return vcv.GetAxis(A, player);
        }
    }
}