using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;

public class SteamInputCore : MonoBehaviour
{
    private static SteamInputCore inputManager;

    [SerializeField] private SteamVR_Action_Boolean triggerPull;
    [SerializeField] private SteamVR_Action_Boolean aPressed;
    [SerializeField] private SteamVR_Input_Sources leftHandController;
    [SerializeField] private SteamVR_Input_Sources rightHandController;
    [SerializeField] public SteamVR_Action_Vibration vibration;

    public static SteamInput GetInput()
    {
        if (inputManager == null)
        {
            Debug.LogError("Input manager has not neen initialised");
        }
        return new(inputManager);
    }

    public void Awake()
    {
        if (inputManager != null)
        {
            Debug.LogError("Input manager has already been initialised");
        }
        inputManager = this;
        DontDestroyOnLoad(gameObject);
    }

    public class SteamInput
    {
        static private SteamInputCore si;
        private ArrayByEnum<Button, bool> leftControllerState = new();
        private ArrayByEnum<Button, bool> rightControllerState = new();
        private ArrayByEnum<Button, bool> leftControllerPressedState = new();
        private ArrayByEnum<Button, bool> rightControllerPressedState = new();
        private ArrayByEnum<Button, bool> leftControllerUnPressedState = new();
        private ArrayByEnum<Button, bool> rightControllerUnPressedState = new();

        public SteamInput(SteamInputCore steamInput)
        {
            si = steamInput;

            si.aPressed[si.leftHandController].onStateDown += ALStateDownCallback;
            si.aPressed[si.rightHandController].onStateDown += ARStateDownCallback;
            si.triggerPull[si.leftHandController].onStateDown += TLStateDownCallback;
            si.triggerPull[si.rightHandController].onStateDown += TRStateDownCallback;
            
            si.aPressed[si.leftHandController].onStateUp += ALStateUpCallback;
            si.aPressed[si.rightHandController].onStateUp += ARStateUpCallback;
            si.triggerPull[si.leftHandController].onStateUp += TLStateUpCallback;
            si.triggerPull[si.rightHandController].onStateUp += TRStateUpCallback;
        }

        public void Destroy()
        {
            si.aPressed[si.leftHandController].onStateDown -= ALStateDownCallback;
            si.aPressed[si.rightHandController].onStateDown -= ARStateDownCallback;
            si.triggerPull[si.leftHandController].onStateDown -= TLStateDownCallback;
            si.triggerPull[si.rightHandController].onStateDown -= TRStateDownCallback;

            si.aPressed[si.leftHandController].onStateUp -= ALStateUpCallback;
            si.aPressed[si.rightHandController].onStateUp -= ARStateUpCallback;
            si.triggerPull[si.leftHandController].onStateUp -= TLStateUpCallback;
            si.triggerPull[si.rightHandController].onStateUp -= TRStateUpCallback;
        }

        private void ALStateDownCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            leftControllerState[Button.A] = true;
            leftControllerPressedState[Button.A] = true;
        }

        private void ARStateDownCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            rightControllerState[Button.A] = true;
            rightControllerPressedState[Button.A] = true;
        }

        private void TLStateDownCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            leftControllerState[Button.Trigger] = true;
            leftControllerPressedState[Button.Trigger] = true;
        }

        private void TRStateDownCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            rightControllerState[Button.Trigger] = true;
            rightControllerPressedState[Button.Trigger] = true;
        }

        private void ALStateUpCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            leftControllerState[Button.A] = false;
            leftControllerUnPressedState[Button.A] = true;
        }

        private void ARStateUpCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            rightControllerState[Button.A] = false;
            rightControllerUnPressedState[Button.A] = true;
        }

        private void TLStateUpCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            leftControllerState[Button.Trigger] = false;
            leftControllerUnPressedState[Button.Trigger] = true;

        }

        private void TRStateUpCallback(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            rightControllerState[Button.Trigger] = false;
            rightControllerUnPressedState[Button.Trigger] = true;
        }

        public bool GetInputDown(Hand h, Button b)
        {
            if (h == Hand.Left)
            {
                var state = leftControllerPressedState[b];
                leftControllerPressedState[b] = false;
                return state;
            } else if (h == Hand.Right)
            {
                var state = rightControllerPressedState[b];
                rightControllerPressedState[b] = false;
                return state;
            }
            Debug.LogWarning("Input doesn't exist");
            return false;
        }

        public bool GetInputUp(Hand h, Button b)
        {
            if (h == Hand.Left)
            {
                var state = leftControllerUnPressedState[b];
                leftControllerUnPressedState[b] = false;
                return state;
            }
            else if (h == Hand.Right)
            {
                var state = rightControllerUnPressedState[b];
                rightControllerUnPressedState[b] = false;
                return state;
            }
            Debug.LogWarning("Input doesn't exist");
            return false;
        }

        public bool GetInput(Hand h, Button b)
        {
            if (h == Hand.Left)
            {
                return leftControllerState[b];
            }
            else if (h == Hand.Right)
            {
                return rightControllerState[b];
            }
            Debug.LogWarning("Input doesn't exist");
            return false;
        }

        public void Vibrate(Hand h, float duration, float frequency, float amplitude)
        {
            if (h == Hand.Left)
            {
                si.vibration.Execute(0, duration, frequency, amplitude, si.leftHandController);
            } else if (h == Hand.Right)
            {
                si.vibration.Execute(0, duration, frequency, amplitude, si.rightHandController);
            }
        }
    }

    public enum Hand { Left, Right };

    public enum Button { A, Trigger };

    // taken from https://stackoverflow.com/questions/981776/using-an-enum-as-an-array-index-in-c-sharp
    // from https://stackoverflow.com/users/127670/ian-goldby answer
    private class ArrayByEnum<U, T> : IEnumerable where U : Enum
    {
        private readonly T[] _array;
        private readonly int _lower;

        public ArrayByEnum()
        {
            _lower = Convert.ToInt32(Enum.GetValues(typeof(U)).Cast<U>().Min());
            int upper = Convert.ToInt32(Enum.GetValues(typeof(U)).Cast<U>().Max());
            _array = new T[1 + upper - _lower];
        }

        public T this[U key]
        {
            get { return _array[Convert.ToInt32(key) - _lower]; }
            set { _array[Convert.ToInt32(key) - _lower] = value; }
        }

        public IEnumerator GetEnumerator()
        {
            return Enum.GetValues(typeof(U)).Cast<U>().Select(i => this[i]).GetEnumerator();
        }
    }
}
