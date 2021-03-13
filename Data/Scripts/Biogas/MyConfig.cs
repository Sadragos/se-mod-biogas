using System;
using System.Collections.Generic;
using ProtoBuf;
using System.Xml.Serialization;
using VRageMath;
using VRage.Game;
using System.Text;

namespace Biogas
{
    [ProtoContract]
    [Serializable]
    public class MyConfig
    {
        [ProtoMember(1)]
        public float PoopChancePerSecond;
        [ProtoMember(2)]
        public float PoopAmountPerSecondMin;
        [ProtoMember(3)]
        public float PoopAmountPerSecondMax;
        [ProtoMember(4)]
        public float PoopAlwaysAt;
        [ProtoMember(5)]
        public float PoopMultiplierSit;
        [ProtoMember(6)]
        public float PoopMultiplierWalk;
        [ProtoMember(7)]
        public float PoopMultiplierFly;
        [ProtoMember(8)]
        public float PoopMultiplierSprint;
        [ProtoMember(9)]
        public float PoopMultiplierCrouch;
        [ProtoMember(10)]
        public bool PoopSounds;
        [ProtoMember(11)]
        public float PoopMultiplierToilet;
        [ProtoMember(12)]
        public float OrganicPerOxyenfarmPerSecondMin;
        [ProtoMember(13)]
        public float OrganicPerOxyenfarmPerSecondMax;

    }


}