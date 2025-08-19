using Entities;
using Entities.Enums;
using Gemini.NET;
using Gemini.NET.Helpers;
using Helper;
using Models.Enums;
using System.Text;

namespace Events;

public static class QuizScope
{
    public const sbyte MaxTotalWordsOfTopic = 10;
    public const sbyte MinTotalQuestions = 10;
    public const sbyte MaxTotalQuestions = 100;
    public const int ThreeDaysAsCachingAge = 259200;
    public const int MaxTimeAsCachingAge = int.MaxValue;
    public const string Instruction = @"
You are an expert English teacher with over 20 years of teaching experience, and you have spent more than 10 years teaching English to high school students in Vietnam. Your deep understanding of language learning challenges allows you to create highly effective, engaging, and pedagogically sound multiple-choice questions. Below are the detailed requirements for the question set generation:

### 1. **English Proficiency Level**:
   - I will provide my English proficiency level according to the CEFR (Common European Framework of Reference for Languages), which will fall into one of the following categories:
     - **A1**: Beginner (simple sentences, basic vocabulary, greetings, introductions).
     - **A2**: Elementary (basic understanding of short texts, simple phrases and expressions).
     - **B1**: Intermediate (understands main points of clear standard input on familiar topics, can handle most situations).
     - **B2**: Upper-intermediate (can produce clear, detailed text on familiar and unfamiliar subjects).
     - **C1**: Advanced (can produce well-structured and detailed text on complex subjects, understanding implicit meanings).
     - **C2**: Proficient (near-native fluency, understanding highly detailed and complex texts).
   
   - Based on the level I provide, your task is to **generate questions appropriate to that level**. For example:
     - **A1**: Short, simple questions with basic vocabulary.
     - **B2**: Complex questions involving conditional sentences, more challenging vocabulary, and topics that require deeper understanding.

### 2. **Question Set Generation Guidelines**:
   - **Clarity and Precision**: The questions should be **clear, direct, and unambiguous**. Avoid using unnecessarily complicated language. Each question should be grammatically correct and easy to understand for the given proficiency level.
   - **Question Types**: Focus on practical, real-world scenarios. Examples of types of questions:
     - **Vocabulary**: Asking for meanings of common words or phrases.
     - **Grammar**: Correct usage of tenses, articles, prepositions, etc.
     - **Contextual Understanding**: Questions that involve understanding the main ideas of simple or complex texts.
     - **Practical Situations**: Everyday conversation topics (e.g., ordering food, booking a hotel, etc.).
   
   - **Choices**: For each question, provide **4 unique choices**. One choice must be the **correct** answer, and the remaining three should be plausible but incorrect answers.
     - The choices should be logically consistent and should not introduce ambiguity.
     - Ensure that the incorrect options are not obvious mistakes but are reasonable distractors based on common learner errors.
   
   - **Correct Answer**: Ensure the correct answer is indisputable. Do not make the question too easy or too tricky.

### 3. **Explanation of Correct Answer**:
   - After each question, provide a **brief explanation in Vietnamese** for why the correct answer is right. The explanation should be:
     - **Clear and concise**, suitable for the proficiency level.
     - **Avoid overwhelming details**; focus on the key learning points.
     - Provide **examples or context** if needed to make the explanation clearer. For instance, if the correct answer involves a specific grammar point, explain that with a simple example.
   - If the explanation requires a specific language rule, be sure to give a short rule or exception (e.g., usage of ""a"" vs. ""an"" or the difference between present perfect and past simple).

### 4. **Priority in Question Generation**:
   - **Engagement**: Questions should be engaging and reflect real-world scenarios that are interesting and useful for language learners. For example, instead of asking about random vocabulary, relate it to daily life (e.g., “What do you usually eat for breakfast?”).
   - **Clarity and Consistency**: The explanations, choices, and the reasoning behind the correct answers should all be **consistent** and **easy to follow**.
   - **Motivation**: Keep the questions positive and encouraging. If the question or explanation is too difficult, adjust the difficulty to motivate further learning.

## Output Format:

### Structured in JSON Format:
   - Return your response in a **valid JSON array**, each object containing the following fields:
     - `Question`: The question text in English. Ensure it is grammatically correct and clearly stated for the given level.
     - `Options`: A list of unique choices (up to 6 choices), where only one choice is the correct answer. Each choice should be a valid option in the context of the question.
     - `RightOptionIndex`: The **index** of the correct answer in the `Options` list. Ensure this index is correct based on the correct choice.
     - `ExplanationInVietnamese`: A **brief explanation** of why the correct answer is correct, written in simple, clear Vietnamese.
   
   - Ensure that the **JSON structure is properly formatted** and valid, adhering to JSON syntax conventions.

### Example Output:

```json
[
    {
        ""Question"": ""What is the capital of Japan?"",
        ""Options"": [""Seoul"", ""Beijing"", ""Tokyo"", ""Bangkok""],
        ""RightOptionIndex"": 2,
        ""ExplanationInVietnamese"": ""Tokyo là thủ đô của Nhật Bản.""
    },
    {
        ""Question"": ""Which of the following is a vegetable?"",
        ""Options"": [""Potato"", ""Apple"", ""Chicken"", ""Cake""],
        ""RightOptionIndex"": 0,
        ""ExplanationInVietnamese"": ""'Potato' là một loại rau củ, khác với các loại thực phẩm còn lại.""
    }
]
```";

    public static async Task<List<Quiz>> GenerateQuizes(string apiKey, string topic, List<AssignmentType> quizzTypes, EnglishLevel level, short questionsCount)
    {
        if (questionsCount <= 15)
        {
            var results = await GenerateQuizesForLessThan15(apiKey, topic, quizzTypes, level, questionsCount);

            if (results == null || results.Count == 0)
            {
                throw new InvalidOperationException("Error while executing");
            }

            return results
                .Take(questionsCount)
                .Select(q => new Quiz
                {
                    Question = q.Question.Replace("**", "'"),
                    Options = q.Options.Select(o => o.Replace("**", "'")).ToList(),
                    RightOptionIndex = q.RightOptionIndex,
                    ExplanationInVietnamese = q.ExplanationInVietnamese.Replace("**", "'"),
                })
                .ToList();
        }
        else
        {
            var quizes = new List<Quiz>();
            var quizTypeQuestionCount = GeneralHelper.GenerateRandomNumbers(quizzTypes.Count, questionsCount);
            var tasks = new List<Task<List<Quiz>>>();

            for (int i = 0; i < quizTypeQuestionCount.Count; i++)
            {
                tasks.Add(GenerateQuizesByType(apiKey, topic, (AssignmentType)(i + 1), level, quizTypeQuestionCount[i]));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result != null && result.Count != 0)
                {
                    quizes.AddRange(result);
                }
            }

            var random = new Random();

            return quizes.Count == 0 ? quizes : quizes
                .AsParallel()
                .OrderBy(x => random.Next())
                .Select(q => new Quiz
                {
                    Question = q.Question.Replace("**", "'"),
                    Options = q.Options.Select(o => o.Replace("**", "'")).ToList(),
                    RightOptionIndex = q.RightOptionIndex,
                    ExplanationInVietnamese = q.ExplanationInVietnamese.Replace("**", "'"),
                })
                .ToList();
        }
    }

    private static async Task<List<Quiz>> GenerateQuizesForLessThan15(string apiKey, string topic, List<AssignmentType> quizzTypes, EnglishLevel level, int questionsCount)
    {
        try
        {
            var userLevel = GeneralHelper.GetEnumDescription(level);
            var types = string.Join(", ", quizzTypes.Select(t => GeneralHelper.GetEnumDescription(t)).ToList());
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine($"I am a English learner with the English proficiency level of `{userLevel}` according to the CEFR standard.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## The decription of my level according to the CEFR standard:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(GetLevelDescription(level));
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Your task:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Generate a set of multiple-choice English questions consisting of {questionsCount} to {questionsCount + 5} questions related to the topic '{topic.Trim()}' for me to practice, the quiz should be of the types: {types}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("The generated questions should be of the types:");
            foreach (var type in quizzTypes)
            {
                promptBuilder.AppendLine($"- {GeneralHelper.GetEnumDescription(type)}");
            }

            var generator = new Generator(apiKey);

            var apiRequest = new ApiRequestBuilder()
                .WithSystemInstruction(Instruction)  // Context có sẵn
                .WithPrompt(promptBuilder.ToString())  // Yêu Cầu cụ Thể
                .WithDefaultGenerationConfig(0.5F, ResponseMimeType.Json)
                .DisableAllSafetySettings()
                .Build();

            var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Lite);

            return [.. JsonHelper.AsObject<List<Quiz>>(response.Result)];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<List<Quiz>> GenerateQuizesByType(string apiKey, string topic, AssignmentType quizzType, EnglishLevel level, int questionsCount)
    {
        try
        {
            var promptBuilder = new StringBuilder();
            var userLevel = GeneralHelper.GetEnumDescription(level);
            var type = GeneralHelper.GetEnumDescription(quizzType);

            promptBuilder.AppendLine($"I am a Vietnamese learner with the English proficiency level of `{userLevel}` according to the CEFR standard.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## The decription of my level according to the CEFR standard:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(GetLevelDescription(level));
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## Your task:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Generate a set of multiple-choice English questions consisting of {questionsCount} to {questionsCount + 5} questions related to the topic '{topic.Trim()}' for me to practice, the type of the questions must be: {type}");

            var generator = new Generator(apiKey);

            var apiRequest = new ApiRequestBuilder()
                .WithSystemInstruction(Instruction)
                .WithPrompt(promptBuilder.ToString())
                .WithDefaultGenerationConfig(0.5F, ResponseMimeType.Json)
                .DisableAllSafetySettings()
                .Build();

            var response = await generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Lite);

            return JsonHelper.AsObject<List<Quiz>>(response.Result)
                .Take(questionsCount)
                .Select(quiz =>
                {
                    quiz.Question = $"({NameAttribute.GetEnumName(quizzType)}) {quiz.Question}";
                    return quiz;
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

   

    private static string GetLevelDescription(EnglishLevel level)
    {
        var A1_Description = @"Level A1 (Beginner)
            Grammar:
            - **Verb 'to be' and 'to have'**: Used for simple present tense sentences to describe identity, location, or possession (e.g., 'I am a teacher', 'She has a book', 'He is at home').
            - **Present Simple Tense**: Used for habits and routines, forming sentences with subjects and basic verbs (e.g., 'I eat breakfast every day', 'She goes to school').
            - **Yes/No Questions with 'to be' and 'do'**: Forming basic Yes/No questions using auxiliary verbs (e.g., 'Are you a student?', 'Do you like coffee?').
            - **Wh- Questions (What, Where, When)**: Using Wh- questions to ask for information (e.g., 'Where is the bus stop?', 'What time is it?').
            - **Articles (a, an, the)**: Correct use of definite and indefinite articles before singular countable nouns (e.g., 'a cat', 'an apple', 'the book').
            - **Simple Prepositions of Place**: Understanding and using 'in', 'on', 'under', 'next to' to describe location (e.g., 'The cat is under the table').
            - **Basic Adjectives**: Learning adjectives to describe size, color, and appearance (e.g., 'a big house', 'a red apple').

            Grammar Scope:
            - Limited to simple present tense.
            - Simple sentence structure (subject + verb + object).
            - Basic questions and negations (e.g., 'I don't like apples').

            Vocabulary Range:
            - **Everyday Vocabulary**: Basic words for everyday situations such as food, family, personal information.
            - **Nouns**: Names of common objects (e.g., 'table', 'car', 'house').
            - **Verbs**: Frequently used action verbs (e.g., 'eat', 'drink', 'walk').
            - **Adjectives**: Common descriptive words (e.g., 'good', 'bad', 'hot', 'cold').
            - **Topics**: Family, basic needs, work and jobs, daily routines, preferences.";

      

        return level switch
        {
            EnglishLevel.Beginner => A1_Description,
            _ => string.Empty,
        };
    }
}
