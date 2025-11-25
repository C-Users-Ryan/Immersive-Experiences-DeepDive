using UnityEngine;
using System.Collections.Generic;

public class PinManager : MonoBehaviour
{
    public PinController[] pins;
    public Transform[] pinResetPositions;

    public int GetPinsDown()
    {
        int count = 0;
        foreach (var pin in pins)
        {
            if (pin.isDown) count++;
        }
        return count;
    }

    public void ResetPins()
    {
        for (int i = 0; i < pins.Length; i++)
        {
            pins[i].ResetPin(pinResetPositions[i]);
        }
    }

    public void PrepareForSecondRoll()
    {
        // Don't reset pins — keep fallen ones down by doing nothing.
    }
}
