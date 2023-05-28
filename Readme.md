# Filigrani's Skill

This is a custom skill API for the game The Long Dark.

## Usage

Register your skill:

```csharp
var testSkill = new SkillDefinition()
{
    Name = "testSkill",
    DisplayNameLocalized = new LocalizedString() { m_LocalizationID = "Test Skill Name" },
    TiersDescriptionLocalized = new[] {
        new LocalizedString() { m_LocalizationID = "Test skill desc 1" },
        new LocalizedString() { m_LocalizationID = "Test skill desc 2" },
        new LocalizedString() { m_LocalizationID = "Test skill desc 3" },
        new LocalizedString() { m_LocalizationID = "Test skill desc 4" },
        new LocalizedString() { m_LocalizationID = "Test skill desc 5" }
    },
    TiersBenefitsLocalized = new[] {
        new LocalizedString() { m_LocalizationID = "Test skill benefit 1" },
        new LocalizedString() { m_LocalizationID = "Test skill benefit 1\nTest skill benefit 2a" },
        new LocalizedString() { m_LocalizationID = "Test skill benefit 1\nTest skill benefit 2b\n" },
        new LocalizedString() { m_LocalizationID = "Test skill benefit 1\nTest skill benefit 2b\nTest skill benefit 3a" },
        new LocalizedString() { m_LocalizationID = "Test skill benefit 1\nTest skill benefit 2b\nTest skill benefit 3b" }
    },
    TierThresholds = new [] {0, 10, 20, 30, 40}, // this means if you have 15 points you are tier 2.
    IconBackgroundId = "ico_skill_large_firstAid",
    ImageId = "ico_skill_large_firstAid",
    Icon = Texture2D.whiteTexture
};
FiligranisSkills.FiligranisSkills.RegisterSkill(testSkill);
```

Increase skill points:
```csharp
FiligranisSkills.FiligranisSkills.IncreaseSkillPoints("testSkill", 8);
```

Get current skill points and tiers:
```csharp
FiligranisSkills.FiligranisSkills.GetPointsAndTiers("testSkill");
```

## Dependencies

- [dommrogers's ModData](https://github.com/dommrogers/ModData/)