using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace simplebibleapp.LinguisticEngine.Models
{
    public record TargetSelectionDto(
        [property: JsonPropertyName("anchor_strongs")] string AnchorStrongs,
        [property: JsonPropertyName("anchor_lemma")] string AnchorLemma,
        [property: JsonPropertyName("transliteration")] string Transliteration,
        [property: JsonPropertyName("contextual_gloss")] string ContextualGloss
    );

    public record ExegeticalContextDto(
        [property: JsonPropertyName("immediate_context_role")] string ImmediateContextRole,
        [property: JsonPropertyName("local_thematic_function")] string LocalThematicFunction,
        [property: JsonPropertyName("canonical_trajectory_summary")] string CanonicalTrajectorySummary
    );

    public record SynonymNodeDto(
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("strongs")] string Strongs,
        [property: JsonPropertyName("lemma")] string Lemma,
        [property: JsonPropertyName("transliteration")] string Transliteration,
        [property: JsonPropertyName("primary_gloss")] string PrimaryGloss,
        [property: JsonPropertyName("semantic_domain")] string SemanticDomain,
        [property: JsonPropertyName("relationship")] string Relationship,
        [property: JsonPropertyName("contextual_confidence")] double ContextualConfidence,
        [property: JsonPropertyName("thematic_alignment_notes")] string ThematicAlignmentNotes
    );

    public record AgyLinguisticPayloadDto(
        [property: JsonPropertyName("target_selection")] TargetSelectionDto TargetSelection,
        [property: JsonPropertyName("exegetical_context")] ExegeticalContextDto ExegeticalContext,
        [property: JsonPropertyName("synonym_network")] List<SynonymNodeDto> SynonymNetwork
    );
}
