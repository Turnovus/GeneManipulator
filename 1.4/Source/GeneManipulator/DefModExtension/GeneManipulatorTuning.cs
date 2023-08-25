using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace GeneManipulator
{
    public class GeneManipulatorTuning : DefModExtension
    {
#pragma warning disable CS0649
        public int baseComplexity;
        public int noPowerTicksToEject;

        public float pawnOffsetY;

        public string insertIconPath;
        public string cancelIconPath;

        public HediffDef postTreatmentHediff;
#pragma warning restore CS0649

        private CachedTexture insertIconInt = null;
        private CachedTexture cancelIconInt = null;

        public CachedTexture InsertIcon
        {
            get
            {
                if (insertIconInt == null)
                    insertIconInt = new CachedTexture(insertIconPath);
                return insertIconInt;
            }
        }

        public CachedTexture CancelIcon
        {
            get
            {
                if (cancelIconInt == null)
                    cancelIconInt = new CachedTexture(cancelIconPath);
                return cancelIconInt;
            }
        }
    }
}
