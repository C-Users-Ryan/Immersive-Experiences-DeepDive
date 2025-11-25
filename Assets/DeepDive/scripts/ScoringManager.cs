using UnityEngine;
using System.Collections.Generic;

public class ScoringManager : MonoBehaviour
{
    public List<Frame> frames = new List<Frame>();
    private int currentFrame = 0;

    private void Awake()
    {
        for (int i = 0; i < 10; i++)
            frames.Add(new Frame());
    }

    public void RegisterRoll(int pins)
    {
        Frame frame = frames[currentFrame];
        frame.AddRoll(pins);

        if (frame.IsComplete(currentFrame))
        {
            if (currentFrame < 9)
                currentFrame++;
        }
    }

    public int GetCurrentFrameIndex()
    {
        return currentFrame;
    }

    public bool IsFrameComplete()
    {
        return frames[currentFrame].IsComplete(currentFrame);
    }

    public bool IsGameOver()
    {
        return currentFrame == 9 && frames[9].IsComplete(9);
    }

    public int GetTotalScore()
    {
        int score = 0;
        for (int i = 0; i < 10; i++)
            score += frames[i].GetScore(frames, i);

        return score;
    }
}

[System.Serializable]
public class Frame
{
    public List<int> rolls = new List<int>();

    public void AddRoll(int pins)
    {
        rolls.Add(pins);
    }

    public bool IsStrike() => rolls.Count == 1 && rolls[0] == 10;
    public bool IsSpare() => rolls.Count == 2 && rolls[0] + rolls[1] == 10;

    public bool IsComplete(int frameIndex)
    {
        bool isTenth = (frameIndex == 9);

        if (!isTenth)
        {
            if (IsStrike()) return true;
            if (rolls.Count == 2) return true;
            return false;
        }
        else
        {
            if (rolls.Count == 2 && rolls[0] + rolls[1] < 10) return true;
            if (rolls.Count == 3) return true;
            return false;
        }
    }

    public int GetScore(List<Frame> frames, int index)
    {
        int score = 0;
        foreach (int r in rolls) score += r;

        if (index == 9)
            return score;

        if (IsStrike())
            return 10 + GetNextTwoRolls(frames, index);

        if (IsSpare())
            return 10 + GetNextRoll(frames, index);

        return score;
    }

    private int GetNextRoll(List<Frame> frames, int index)
    {
        if (index + 1 >= frames.Count) return 0;
        return frames[index + 1].rolls.Count > 0 ? frames[index + 1].rolls[0] : 0;
    }

    private int GetNextTwoRolls(List<Frame> frames, int index)
    {
        List<int> next = new List<int>();

        for (int i = index + 1; i < frames.Count; i++)
        {
            next.AddRange(frames[i].rolls);
            if (next.Count >= 2) break;
        }

        while (next.Count < 2) next.Add(0);

        return next[0] + next[1];
    }
}
