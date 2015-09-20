using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace d360.core
{
    public enum FactObject
    {
        [Category("Transitive")]
        Fact = 1,
        [Category("Data")]
        Artifact = 3,
        [Category("Relation")]
        ArtifactSubType = 6,
        [Category("Relation")]
        ArtifactType = 2,
        [Category("Data")]
        Perspective = 4,
        [Category("Relation")]
        PerspectiveType = 7,
        [Category("Data")]
        Intersect = 5,
        [Category("Relation")]
        IntersectType = 8,
        [Category("Data")]
        InheritedIntersect = 9,
        [Category("Relation")]
        QualityRuleType = 10,
        [Category("Data")]
        QualityRule = 11

    }
}
