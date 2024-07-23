using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace MarketingCodingAssignment.Models
{
    public class CustomAnalyzer : Analyzer
    {
        protected override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
        {
            var source = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
            TokenStream filter = new StandardFilter(LuceneVersion.LUCENE_48, source);
            filter = new LowerCaseFilter(LuceneVersion.LUCENE_48, filter);
            filter = new StopFilter(LuceneVersion.LUCENE_48, filter, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
            filter = new PorterStemFilter(filter); // Stemming filter added here
            return new TokenStreamComponents(source, filter);
        }
    }
}
