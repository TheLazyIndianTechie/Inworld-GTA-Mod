using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;

namespace InworldV.Ambiance
{
    internal class PoliceChatter
    {
        private List<string> chatterList = new List<string>() {
                "LAMAR_1_POLICE_LOST",
                "SCRIPTED_SCANNER_REPORT_AH_3B_01",
                "SCRIPTED_SCANNER_REPORT_AH_MUGGING_01",
                "SCRIPTED_SCANNER_REPORT_ARMENIAN_1_01",
                "SCRIPTED_SCANNER_REPORT_ARMENIAN_1_02",
                "SCRIPTED_SCANNER_REPORT_ASS_BUS_01",
                "SCRIPTED_SCANNER_REPORT_ASS_MULTI_01",
                "SCRIPTED_SCANNER_REPORT_BARRY_3A_01",
                "SCRIPTED_SCANNER_REPORT_BS_2A_01",
                "SCRIPTED_SCANNER_REPORT_BS_2B_01",
                "SCRIPTED_SCANNER_REPORT_BS_2B_02",
                "SCRIPTED_SCANNER_REPORT_BS_2B_03",
                "SCRIPTED_SCANNER_REPORT_BS_2B_04",
                "SCRIPTED_SCANNER_REPORT_BS_PREP_A_01",
                "SCRIPTED_SCANNER_REPORT_BS_PREP_B_01",
                "SCRIPTED_SCANNER_REPORT_BS_PREP_A_01",
                "SCRIPTED_SCANNER_REPORT_BS_PREP_B_01",
                "SCRIPTED_SCANNER_REPORT_CAR_STEAL_2_01",
                "SCRIPTED_SCANNER_REPORT_CAR_STEAL_4_01",
                "SCRIPTED_SCANNER_REPORT_DH_PREP_1_01",
                "SCRIPTED_SCANNER_REPORT_FIB_1_01",
                "SCRIPTED_SCANNER_REPORT_FIN_C2_01",
                "SCRIPTED_SCANNER_REPORT_Franklin_2_01",
                "SCRIPTED_SCANNER_REPORT_FRANLIN_0_KIDNAP",
                "SCRIPTED_SCANNER_REPORT_GETAWAY_01",
                "SCRIPTED_SCANNER_REPORT_JOSH_3_01",
                "SCRIPTED_SCANNER_REPORT_JOSH_4_01",
                "SCRIPTED_SCANNER_REPORT_JSH_2A_01",
                "SCRIPTED_SCANNER_REPORT_JSH_2A_02",
                "SCRIPTED_SCANNER_REPORT_JSH_2A_03",
                "SCRIPTED_SCANNER_REPORT_JSH_2A_04",
                "SCRIPTED_SCANNER_REPORT_JSH_2A_05",
                "SCRIPTED_SCANNER_REPORT_JSH_PREP_1A_01",
                "SCRIPTED_SCANNER_REPORT_JSH_PREP_1B_01",
                "SCRIPTED_SCANNER_REPORT_JSH_PREP_2A_01",
                "SCRIPTED_SCANNER_REPORT_JSH_PREP_2A_02",
                "SCRIPTED_SCANNER_REPORT_LAMAR_1_01",
                "SCRIPTED_SCANNER_REPORT_MIC_AMANDA_01",
                "SCRIPTED_SCANNER_REPORT_NIGEL_1A_01",
                "SCRIPTED_SCANNER_REPORT_NIGEL_1D_01",
                "SCRIPTED_SCANNER_REPORT_PS_2A_01",
                "SCRIPTED_SCANNER_REPORT_PS_2A_02",
                "SCRIPTED_SCANNER_REPORT_PS_2A_03",
                "SCRIPTED_SCANNER_REPORT_SEC_TRUCK_01",
                "SCRIPTED_SCANNER_REPORT_SEC_TRUCK_02",
                "SCRIPTED_SCANNER_REPORT_SEC_TRUCK_03",
                "SCRIPTED_SCANNER_REPORT_SIMEON_01",
                "SCRIPTED_SCANNER_REPORT_Sol_3_01",
                "SCRIPTED_SCANNER_REPORT_Sol_3_02"
              };

        private bool isStarted;

        public PoliceChatter()
        {
        }

        public void StartChatter()
        {
            if (!isStarted)
            {

                isStarted = true;
            }
        }

        public void StopChatter()
        {
            if (isStarted)
            {
                isStarted = false;
            }
        }

        public void OnTick(object sender, EventArgs e)
        {
            PlayRandomChatter();
        }

        private void PlayRandomChatter()
        {
            Ped character = Game.Player.Character;
            int wantedLevel = Game.Player.WantedLevel;
            Random random = new Random();
            if (!character.IsAlive || wantedLevel >= 1)
                return;
            int index = random.Next(chatterList.Count);
            Function.Call(Hash.PLAY_POLICE_REPORT, new InputArgument[1]
            {
                (InputArgument) chatterList[index]
            });
            Script.Wait(random.Next(2000, 11000));
        }
    }

}
