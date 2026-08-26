using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Customization.Domain;

public sealed class LandingSection : AggregateRoot<Guid>
{
    public string SectionType { get; private set; } = string.Empty; // Hero, StatsCounter, FeaturedCourses, FeaturesGrid, Testimonials, FaqAccordion, CtaBanner
    public string? Title { get; private set; }
    public string? Subtitle { get; private set; }
    public int OrderIndex { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;
    public string ConfigJson { get; private set; } = "{}";

    private LandingSection() { }

    public LandingSection(
        Guid id,
        string sectionType,
        string? title,
        string? subtitle,
        int orderIndex,
        bool isActive,
        string configJson) : base(id)
    {
        SectionType = sectionType;
        Title = title;
        Subtitle = subtitle;
        OrderIndex = orderIndex;
        IsActive = isActive;
        ConfigJson = configJson;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static LandingSection Create(
        string sectionType,
        string? title,
        string? subtitle,
        int orderIndex,
        bool isActive,
        string configJson)
    {
        return new LandingSection(
            Guid.NewGuid(),
            sectionType,
            title,
            subtitle,
            orderIndex,
            isActive,
            configJson);
    }

    public void Update(
        string? title,
        string? subtitle,
        int orderIndex,
        bool isActive,
        string configJson)
    {
        Title = title;
        Subtitle = subtitle;
        OrderIndex = orderIndex;
        IsActive = isActive;
        ConfigJson = configJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetOrderIndex(int orderIndex)
    {
        OrderIndex = orderIndex;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
