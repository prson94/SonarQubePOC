using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace d360.jobs.AnalyzeCloudFusionData.eagle.messageCenter
{
    public class BloombergMapList : MapList
    {

        protected override IRelationshipMapping CreateRelationship(string tag, string expression, string description)
        {
            //we need to handle interpretation of the expression

            var relationship = new  BloombergRelationshipMapping { Tag = tag, Expression = expression, UserDescription = description };

            //need to evaluate the relationship

            interpretRelationshipTagValue(relationship);

            parseExpression(relationship);
            
            return relationship;
        }


        private void parseExpression(BloombergRelationshipMapping rel)
        {
            //determine what we are dealing with
            DetermineExpressionValueType(rel);

            switch (rel.ExpressionValueType)
            {
                case RelationshipExpressionType.ConstantValue:
                    rel.ConstantValue = rel.Expression;
                    break;
                case RelationshipExpressionType.DirectMapping:
                    //some direct mappings are referneces to prior mappings tags.  We must check if the expression is the tag on a prior column
                    //if (Regex.IsMatch(rel.Expression, ":tag(.*?):"))
                    if(!isExressionValueAPriorTag(rel))                    
                        rel.BloombergMnemonics.Add(rel.Expression.Trim(':')); //single mnemonic value
                    break;
                case RelationshipExpressionType.ConditionalMapping:
                    extractMnemonics(rel);
                    break;
                case RelationshipExpressionType.Unknown:
                    break;
                default:
                    break;
            }
        }


        private void DetermineExpressionValueType(BloombergRelationshipMapping rel)
        {
            // if first and last character is : then its a direct bloomberg mnemonic
            // if first and last character is ' then its a constant value
            if (string.IsNullOrEmpty(rel.Expression))
                rel.ExpressionValueType = RelationshipExpressionType.Unknown;
            //else if (rel.Expression[0] == '\'' && rel.Expression[rel.Expression.Length - 1] == '\'')
            else if (Regex.IsMatch(rel.Expression, "^\'(.*?)\'$")) // a expression starting and ending in " with leters or numbers or spaces inbetween
                rel.ExpressionValueType = RelationshipExpressionType.ConstantValue;            
            else if (Regex.IsMatch(rel.Expression, "^:([a-zA-Z0-9.,$;-_]+):$")) // a expression starting and ending in : with either a constant or bb mnemonic
                rel.ExpressionValueType = RelationshipExpressionType.DirectMapping;
            else
                rel.ExpressionValueType = RelationshipExpressionType.ConditionalMapping;
        }

        private bool isExressionValueAPriorTag(BloombergRelationshipMapping rel)
        {
            bool isTagReference = false;

            var val = rel.Expression.Trim(':');
            val = val.ToUpper();

            if(val.StartsWith("TAG",StringComparison.InvariantCulture))
            {
                isTagReference = true;

                val = val.Replace("TAG", ""); //sometime they start the value with TAG...
            }
                     
            uint tempUInt = 0;
            if (uint.TryParse(val, out tempUInt))
                isTagReference = true;

            
            var ex = this.Find(x => x.Tag == val);

            if (ex == null && isTagReference) return true; // just ingore this as its a tag reference and we cant find it            
            if (ex == null) return false;

            rel.Expression = ex.Expression;

            //need to recheck call parse expression to update any properties that may change as a result of this reference to another expression 
            parseExpression(rel);

            return true; //caution recursive
        }
        
        private void extractMnemonics(BloombergRelationshipMapping rel)
        {
          //  return; // dont want to do this yet as it will take things in if(my value) then my value 2 

            //run regexpression to find all 
            var matches = Regex.Matches(rel.Expression, ":([A-Z0-9-_]+):");

            foreach (var match in matches)
            {
                var item = match.ToString().Trim(':');

                if(!rel.BloombergMnemonics.Contains(item))
                    rel.BloombergMnemonics.Add(item);
            }

        }
    }
}
