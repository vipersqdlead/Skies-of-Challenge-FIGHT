using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillCounter : MonoBehaviour
{
    public int Kills = 0;
    public int Points = 0;

    public bool comboCounting;
    public int currentCombo;

    private void Update()
    {

    }

    public void GiveKill(bool countsAsKill, int points)
    {
        if (countsAsKill)
        {
            Kills += 1;
        }
        currentCombo += 1;
        Points += (points * (currentCombo + 1));
        comboCounting = true;
        return;
    }

    public void GivePoints(int points)
    {
        Points += (points * (currentCombo + 1));
        return;
    }

    public void StopCombo()
    {
        currentCombo = 1;
        comboCounting = false;
    }
}
