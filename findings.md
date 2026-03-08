# What I Found in the eCFR Data

I spent time before building the app just exploring the raw data from the eCFR API. I wanted to understand what the regulations actually look like at scale before designing metrics around them. Here's what stood out.

---

## The size of the CFR

The full CFR comes out to about 105.3 million words across 49 active titles. To put that in perspective, that's about 1,300 novels.

But the thing that surprised me is how concentrated it is. The top 5 titles make up 42% of all regulation:

| Rank | Title | Words | % of CFR |
|------|-------|-------|----------|
| 1 | Title 40 - Protection of Environment | 16,873,429 | 16.0% |
| 2 | Title 26 - Internal Revenue | 12,037,628 | 11.4% |
| 3 | Title 12 - Banks and Banking | 5,672,217 | 5.4% |
| 4 | Title 7 - Agriculture | 5,639,605 | 5.4% |
| 5 | Title 49 - Transportation | 4,108,187 | 3.9% |

EPA alone has more words than the next two titles combined. If we were thinking about where to focus deregulation, those 5 titles are where almost half the weight sits.

---

## These regulations are barely readable

I ran some readability analysis on the XML text and the numbers were worse than I expected.

The average reading grade across the CFR is 12.2, so high school senior level. But securities regulations (Title 17) clock in at grade 18.7, basically requiring a graduate degree to parse. Banking (Title 12) isn't far behind at 17.5. For reference, newspapers target a grade 8 reading level.

| Metric | Value |
|--------|-------|
| Average CFR reading grade | 12.2 (high school senior) |
| Hardest: Title 17 - Securities | Grade 18.7 (graduate level) |
| Second: Title 12 - Banking | Grade 17.5 (graduate level) |
| Newspaper target | Grade 8 |

I also found some really long sentence lengths. Title 12 has a single sentence that runs 1,539 words. Securities has one at 721 words. Over 3,200 sentences in my sample exceeded 100 words.

| Title | Avg Words/Sentence | Longest Sentence | % Over 50 Words |
|-------|--------------------|-----------------|-----------------|
| Title 17 - Securities | 39.1 | 721 words | 25.2% |
| Title 12 - Banking | 34.5 | 1,539 words | 20.9% |
| Title 6 - Domestic Security | 30.4 | 443 words | 15.1% |
| Title 24 - Housing | 26.1 | 333 words | 14.1% |

Another thing I noticed while reading through the XML is how much passive voice there is. I wrote a quick regex to count "be + past participle" patterns and got 853,017 hits across the CFR. That works out to one every 123 words. Instead of saying "the applicant must submit Form X," regulations say "Form X shall be submitted." It sounds minor but when you're trying to figure out who's responsible for what, that kind of language adds up fast.

---

## The cross-reference problem

This one was eye-opening. The regulations don't exist, in an isolated manner. They reference each other 733,908 times internally, creating this web of dependencies where understanding one rule means reading a dozen others.

| Title | Total Refs | Density |
|-------|-----------|---------|
| Title 40 - Environment | 155,099 | 9.2/1000w |
| Title 26 - Internal Revenue | 55,365 | 4.6/1000w |
| Title 12 - Banking | 39,865 | 7.0/1000w |
| Title 46 - Shipping | 19,246 | 10.0/1000w |

Title 40 has a cross-reference roughly every 109 words. And that's just internal. There are another 446,940 external citations pointing to the Federal Register, U.S. Code, Executive Orders, Public Laws, and OMB Circulars.

| External Source | Citations |
|----------------|-----------|
| Federal Register | 326,018 |
| U.S. Code (USC) | 108,677 |
| Executive Orders | 3,281 |
| Public Laws | 2,780 |
| OMB Circulars | 2,590 |

So to fully understand one regulation, you would probably need to chase references across five different legal systems. That's a real barrier to compliance.

### Rules that contradict themselves

This one was interesting. I searched for mandatory language ("shall" and "must") and found 894,858 instances across the CFR. Then I searched for escape language, things like "except as," "exemption," "waiver," and "notwithstanding," and found 170,120 of those. So for roughly every 5 mandatory commands, there's an exception or workaround built in.

| Term | Occurrences |
|------|------------|
| "except as" | 36,004 |
| "exemption(s)" | 30,685 |
| "exception(s)" | 26,845 |
| "waiver(s)" | 19,961 |
| "notwithstanding" | 9,435 |

Immigration law (Title 8) stood out with 918 waivers alone. Basically the regulations tell you what you have to do, and then right next to it there's a list of ways you don't actually have to do it. That makes enforcement inconsistent and compliance confusing for everyone involved.

---

## Transition surges and a data lesson

This is where the version history data got interesting, and where I almost got burned by a data artifact.

My first pass showed a massive 1,575% surge in regulatory changes during presidential transitions. The 2016-2017 period alone showed 219,927 changes in three months. That seemed too dramatic, so I dug in further.

It turns out 37 of the 49 titles had their version history bootstrapped into the eCFR system in late 2016. December 2016 alone has 153,378 entries, and January 1, 2017 has 39,741 entries across 16 titles. Those aren't real regulatory changes. That's the system loading historical data for the first time. Before 2016, only 7 titles had any version history at all.

After filtering out that bootstrap period, the corrected numbers still showed something significant. Regulatory activity increases by about 85% during presidential transitions compared to normal periods. It's not as dramatic as I initially calculated, but it's still notable that outgoing administrations nearly double their regulatory output on the way out.

The only clean comparison is the 2020 and 2024 transitions against non-transition periods from 2017 onward, since the version history before that is too sparse to trust.

| Transition | Changes (Nov-Jan) |
|-----------|-------------------|
| 2024-2025 | 11,291 |
| 2020-2021 | 9,334 |

The quarterly breakdown also looks different after removing the 2016 artifact. Q1 and Q4 are nearly identical at around 28%, which honestly weakens the "Q4 gaming" narrative I was expecting to find. The changes are more evenly distributed than I initially thought.

| Quarter | % of Changes (corrected) |
|---------|------------------------|
| Q1 (Jan-Mar) | ~28% |
| Q2 (Apr-Jun) | ~21% |
| Q3 (Jul-Sep) | ~23% |
| Q4 (Oct-Dec) | ~28% |

I'm glad I did a second pass of the data before presenting it. The text-based findings (word counts, readability, cross-references, mandates) are all from the actual XML content and aren't affected by the version history artifact.

---

## Too many cooks in the kitchen

I looked at how many agencies claim jurisdiction over each title and the overlap is significant.

Title 5 (Administrative Personnel) has 68 agencies writing into it. 26 of the 49 titles have 5 or more agencies involved. The average is 9.1 agencies per title. When that many organizations share ownership of a body of regulation, there's no clear accountability for simplifying it.

| Title | Agencies |
|-------|---------|
| Title 5 - Administrative Personnel | 68 |
| Title 2 - Federal Financial Assistance | 35 |
| Title 48 - Federal Acquisition | 31 |
| Title 7 - Agriculture | 27 |
| Title 22 - Foreign Relations | 18 |

In total there are 316 organizational units (153 top-level agencies plus 163 sub-agencies) writing regulation. USDA alone has 28 sub-agencies, each with its own rulemaking process.

### Zombie regulations

14 agencies have regulations that haven't been substantively updated in over a year, covering about 11.8 million words or 11.2% of the entire CFR.

| Tier | Criteria | Agencies | Words at Risk |
|------|----------|----------|---------------|
| Zombie | 5+ years stale | 1 | 4,223 |
| Stale | 2-5 years | 7 | 476,126 |
| Aging | 1-2 years | 6 | 11,302,461 |

The Executive Office of the President's own regulations haven't been updated since March 2015. That's 11 years.

---

## What the actual burden looks like

I wanted to get a sense of how much the CFR actually demands from people, so I started searching for command language. Across all titles, "shall" appears 488,440 times and "must" appears 406,418 times. That's 894,858 mandatory commands total, or roughly one every 118 words. Mineral resources and shipping regulations are the most demanding per word.

| Title | Density | "Shall" | "Must" | Total |
|-------|---------|---------|--------|-------|
| Title 30 - Mineral Resources | 15.0/1000w | 14,362 | 5,963 | 20,325 |
| Title 46 - Shipping | 14.8/1000w | 9,567 | 19,036 | 28,603 |
| Title 49 - Transportation | 11.8/1000w | 23,681 | 24,603 | 48,284 |

Then I looked at the paperwork side. There are 113,080 mentions of reporting requirements, 26,803 references to specific forms, and 154,036 enforcement terms like "violation," "penalty," and "prohibition." EPA is responsible for a huge chunk of that on its own with 33,623 reporting mentions and 11,715 "submit" requirements. The IRS has 6,297 form references.

One weird thing I noticed is that the CFR has 28,459 [Reserved] sections. These are spots where a regulation was planned or removed but the placeholder was never cleaned up. They just sit there doing nothing. Title 40 alone has 8,612 of them.

The tax code was probably the most unique. Title 26 is the 2nd largest body of regulation at 12 million words and has 93,759 dollar amount references scattered through it. But when I looked at how often it actually gets updated, it has the lowest churn rate of any major title, just 2.72 changes per year per 100k words. Title 41 (Public Contracts) changes 53 times faster. So long story short you have this enormous, complex body of law that basically never gets revised.

---

## Pulling it all together

If I had to summarize what I took away from this, it's that the CFR has three core problems: it's too big, it's too hard to read, and nobody's in charge of fixing it.

The size is concentrated enough that focusing on 5 titles would cover 42% of everything. The language is written at a graduate reading level when most Americans read at an 8th grade level. And the ownership is so fragmented across 316 organizational units that there's no accountability for simplification.

Here are all the numbers in one place:

| What I Measured | What I Found |
|----------------|-------------|
| Total words in the CFR | 105.3 million |
| Top 5 titles share of all regulation | 42.1% |
| Longest sentence I found | 1,539 words |
| Hardest reading level | Grade 18.7 (securities) |
| Passive voice instances | 853,017 |
| Internal cross-references | 733,908 |
| External legal citations | 446,940 |
| Mandates per escape clause | 5.3 to 1 |
| Transition surge (corrected for data artifact) | +85% |
| Changes happening in Q4 (corrected) | ~28% |
| Agencies writing into Title 5 | 68 |
| Words in aging/stale regulations | 11.8 million |
| Mandatory commands (shall/must) | 894,858 |
| Reporting requirements | 113,080 |
| Enforcement terms | 154,036 |
| Empty [Reserved] placeholders | 28,459 |
| Total org units writing regulation | 316 |

---

## How I did this

I wrote a few Python scripts to pull and analyze the data before building the app. The eCFR API is public and doesn't need authentication, so I was able to grab everything directly.

**Data collection**: I downloaded all 49 active titles as XML from the eCFR API (dated 2026-03-04) and cached them locally so I wasn't hitting the API repeatedly. I also pulled the full version history for every title, which gave me 471,437 entries to work with for the temporal analysis.

**Text analysis**: For word counts I extracted all text nodes from the XML using Python's ElementTree. For readability I used the Flesch-Kincaid grade level formula on samples of about 50k words per title. Sentence lengths came from regex-based sentence splitting on 100k word samples per title.

**Pattern matching**: Most of the burden and complexity metrics came from regex searches. I matched "shall" and "must" for mandates, "except as," "exemption," "waiver," and "notwithstanding" for escape clauses, section symbols (§) and part references for cross-references, and "be + past participle" patterns for passive voice. External citations were matched against known patterns for USC, Public Law, Executive Order, OMB Circular, and Federal Register references.

**Agency attribution**: Word counts per agency were calculated proportionally using the cfr_references field from the agency metadata endpoint, which maps each agency to the titles and chapters it regulates.

**Temporal analysis**: I parsed the date field from all 471,437 version history entries to identify transition surges, quarterly patterns, and staleness per title.
