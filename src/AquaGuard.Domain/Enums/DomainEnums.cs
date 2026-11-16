namespace AquaGuard.Domain.Enums;

public enum Gender { Female, Male, Other, PreferNotToSay }
public enum WaterSourceType { TubeWell, Spring, River, Pond, CommunityTap, Other }
public enum TestResult { NotTested, Safe, Unsafe }
public enum QualityStatus { Pending, Safe, AttentionNeeded, Unsafe }
public enum RiskLevel { Low, Moderate, High, Critical }
public enum WarningStatus { Open, Acknowledged, Resolved, Dismissed }
public enum NotificationType { EarlyWarning, WaterQuality, DiseaseCase, System }
