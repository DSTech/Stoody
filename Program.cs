using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Stoody
{
	class Program
	{
		const string QuestionFile = "questions.txt";
		const int numTriesPerQuestion = 3;
		const int knownLevel = 2;

		static Random r = new Random();
		static string[] statements = new string[]
		{
			"Correct!",
			"Yes!",
			"Woot!",
			"That's right!",
			"Keep going, you're doing well!",
			"No problems with that answer!",
			"You will now be provided with an encouraging statement to boost your performance\n<Encouraging statement>"
        };
		static string getEncouragingStatement()
		{
			return statements[r.Next(statements.Length)];
		}
		static void Main(string[] args)
		{
			if (!File.Exists(QuestionFile))
			{
				Console.WriteLine("No questions were loaded, as " + QuestionFile + " does not exist.");
				goto Exit;
			}
			var q = new Questioner(QuestionFile);

			while (true)
			{
				if (!q.nextQuestion())
				{
					Console.WriteLine("We've run out of questions to ask! You're ready for the test :D!");
					goto Exit;
				}
				Console.WriteLine(q.getQuestion());
				int numTimesTried = 0;
				while (true)
				{
					++numTimesTried;
					string ans = Console.ReadLine();
					if (ans.ToLower() == "q" || ans.ToLower() == "quit" || ans.ToLower() == "exit")
					{
						goto Exit;
					}
					if (q.isValidAnswerCaseInsensitive(ans))
					{
						int newscore = q.Award();
						Console.WriteLine("{0}", getEncouragingStatement());
						Console.WriteLine("This question is now at score {0}\n", newscore);
						if (newscore >= knownLevel)
						{
							q.removeQuestion();
							Console.WriteLine("You've gotten a high enough score that this question has been removed!\n\n");
						}
						break;
					}
					else
					{
						Console.Write("That answer was incorrect. ");
						if (numTimesTried >= numTriesPerQuestion)
						{
							int newscore = q.Punish();
							Console.WriteLine("This question is now at score {0}\n", newscore);
							Console.WriteLine("Possible answers were:");
							foreach (var answer in q.getValidAnswers())
							{
								Console.WriteLine("\t{0}", answer);
							}
							break;
						}
						else
						{
							int triesRemaining = numTriesPerQuestion - numTimesTried;
							if (triesRemaining == 1)
							{
								Console.WriteLine("1 attempt left.");
							}
							else
							{
								Console.WriteLine("{0} attempts left.", triesRemaining);
							}
						}
					}
				}
			}
		Exit:
			Console.Write("\nEnding Execution.\n");
		}
	}
	class Question
	{
		public Question(string QuestionString, string Answer)
		{
			this.QuestionString = QuestionString;
			Answers = new List<string>(new string[] { Answer });
		}
		public Question(string QuestionString, string[] Answers)
		{
			this.QuestionString = QuestionString;
			this.Answers = new List<string>(Answers);
		}
		string QuestionString;
		List<string> Answers;
		public override string ToString()
		{
			return QuestionString;
		}
		public string GetQuestionString()
		{
			return this.ToString();
		}
		public bool isValidAnswer(string ans)
		{
			return Answers.Contains(ans);
		}
		public bool isValidAnswerCaseInsensitive(string ans)
		{
			return Answers.Select(x => x.ToLower()).Where(x => x == ans.ToLower()).Count() > 0;
		}
		public string[] getValidAnswers()
		{
			return Answers.ToArray();
		}
	}
	class Questioner
	{
		public Questioner() { }
		public Questioner(string questionFile) { loadQuestions(questionFile); }
		public List<Question> questions = new List<Question>();
		QuestionSelector qsel;
		Question currentQuestion = null;
		public string getQuestion()
		{
			return currentQuestion.GetQuestionString();
		}
		public bool isValidAnswer(string str)
		{
			return currentQuestion.isValidAnswer(str);
		}
		public bool isValidAnswerCaseInsensitive(string str)
		{
			return currentQuestion.isValidAnswerCaseInsensitive(str);
		}
		public string[] getValidAnswers()
		{
			return currentQuestion.getValidAnswers();
		}
		public bool nextQuestion()
		{
			return (currentQuestion = qsel.next(currentQuestion)) != null;
		}
		public int Award()
		{
			return qsel.Award(currentQuestion);
		}
		public void removeQuestion()
		{
			qsel.RemoveStat(currentQuestion);
			currentQuestion = null;
		}
		public int Punish()
		{
			return qsel.Punish(currentQuestion);
		}
		public void addQuestion(string question, string answer)
		{
			addQuestion(question, new string[] { answer });
		}
		public void addQuestion(string question, List<string> answers)
		{
			questions.Add(new Question(question, answers.ToArray()));
		}
		public void addQuestion(string question, string[] answers)
		{
			questions.Add(new Question(question, answers));
		}
		public bool loadQuestions(string filename)
		{
			string[] data;
			try
			{
				TextReader reader = new StreamReader(File.OpenRead(filename));

				data = reader.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select((x) => x.Substring(0,x.IndexOf("//")>=0?x.IndexOf("//"):x.Length)).Where((x) => x.Length > 0).ToArray();
				reader.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to load questions from file {0}, reason:\n\t{1}", filename, e.Message);
				return false;
			}
			int curline = 0;
			Func<string> getNextLine = () => { string ret = data[curline]; ++curline; return ret.Trim(); };
			Func<bool> hasNextLine = () => { return data.Length > curline; };

			while (hasNextLine())
			{
				string tempLine;

				List<string> lQuestions;
				tempLine = getNextLine();
				if (tempLine.StartsWith("#"))
				{
					int numQuestions = int.Parse(tempLine.Substring(1));
					lQuestions = new List<string>(numQuestions);
					for (uint i = 0; i < numQuestions; ++i)
					{
						lQuestions.Add(getNextLine());
					}
				}
				else
				{
					lQuestions = new List<string>(new string[] { tempLine });
				}

				tempLine = getNextLine();
				if (tempLine.StartsWith("#"))
				{
					int numAnswers = int.Parse(tempLine.Substring(1));
					List<string> answers = new List<string>(numAnswers);
					for (uint i = 0; i < numAnswers; ++i)
					{
						answers.Add(getNextLine());
					}
					foreach (var question in lQuestions)
					{
						addQuestion(question, answers);
					}
				}
				else
				{
					string answer = tempLine;
					foreach (var question in lQuestions)
					{
						addQuestion(question, answer);
					}
				}
			}
			qsel = new QuestionSelector(this);
			Console.WriteLine("Loaded {0} question(s) from file {1}...", questions.Count, filename);
			return true;
		}
	}
	class QuestionSelector
	{
		public QuestionSelector(Questioner qr)
		{
			makeStats(qr);
		}
		Dictionary<Question, int> stats;
		public void makeStats(Questioner qr)
		{
			stats = new Dictionary<Question, int>(qr.questions.Count);
			foreach (Question q in qr.questions)
			{
				stats[q] = 0;
			}
		}
		public void RemoveStat(Question q)
		{
			if (stats.ContainsKey(q))
			{
				stats.Remove(q);
			}
		}
		public int Award(Question q)
		{
			return ++stats[q];
		}
		public int Punish(Question q)
		{
			return --stats[q];
		}
		Random r = new Random();
		public Question next(Question lastQuestion)
		{
			int currentLowest = int.MaxValue;
			List<Question> retlist = new List<Question>();
			foreach (Question q in stats.Keys)
			{
				if (stats[q] < currentLowest)
				{
					retlist.Clear();
					if (q != lastQuestion || lastQuestion == null || stats.Keys.Count == 1)
					{
						retlist.Add(q);
						currentLowest = stats[q];
					}
				}
				else if (stats[q] == currentLowest)
				{
					if (q != lastQuestion || lastQuestion == null || stats.Keys.Count == 1)
					{
						retlist.Add(q);
					}
				}
			}
			return (retlist.Count() > 0) ? (retlist[r.Next(retlist.Count())]) : null;
		}
	}
}
