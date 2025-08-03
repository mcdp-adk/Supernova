using System;
namespace _Scripts.Utilities
{
    public enum CellTypeEnum
    {
        None = 0,
        Lava = -1,
        RockVolcanic = -2,
        WoodScorched = -3,
        WoodWet = -4,
        Wood = -5,
        Snow = -6,
        Ice = -7,
        Water = -8,
        Grass = -9,
        Soil = -10,
        Ground = -11,
        GroundDry = -12,
        Sand = -13,
        Concrete = -14,
        StoneBasalt = -15,
        StoneGranite = -16,
        StoneRiver = -17,
        StoneSlate = -18,
        RockBedrock = -19
    }

    public enum CellStateEnum : byte
    {
        None = 0,
        Solid = 1,
        Powder = 2,
        Liquid = 3
    }

    public struct CellConfig
    {
        public CellTypeEnum Type;
        public CellStateEnum State;
        public int Mass;
        public float Fluidity;
        public float Viscosity;
        public float TemperatureDefault;
        public float TemperatureMin;
        public float TemperatureMax;
        public float HeatConductivity;
        public float IgnitionPoint;
        public float ExplosionPoint;
        public float MoistureDefault;
        public float MoistureMin;
        public float MoistureMax;
        public float MoistureConductivity;
        public float EnergyDefault;
        public float DropChanceGold;
        public float DropChanceSilver;
        public float DropChanceCopper;
        public float DropChanceIron;
    }

    [Serializable]
    public struct LayerCellGenerationConfig
    {
        public CellTypeEnum cellType;
        public int weight;
    }

    [Serializable]
    public struct LayerGenerationConfig
    {
        public int radius;
        public float density;
        public int explosionStrength;
        public int explosionAngleClamp;
        public LayerCellGenerationConfig[] cellConfigs;
    }
}