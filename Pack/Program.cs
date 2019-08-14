using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pack
{
	internal class Program
	{
		[STAThread]
		public static int Main(string[] args)
		{
			try
			{
				var gameDir = new DirectoryInfo(args[0]);
				var fileInfos = gameDir.GetFiles("*.cs", SearchOption.AllDirectories).Where(f => !IsExcluded(f)).ToList();
				var result = new StringBuilder();
				result.AppendLine($"// Date: {DateTime.UtcNow}");

				var compact = false;
				var usings = new HashSet<string>();
				var contents = new List<Tuple<FileInfo, string, int>>();
				foreach (var fileInfo in fileInfos)
				{
					Console.Out.WriteLine($"Preprocessing {fileInfo.Name}");
					using (var fileStream = fileInfo.OpenRead())
					using (var fileReader = new StreamReader(fileStream))
					{
						var fileContent = fileReader.ReadToEnd();
						if (IsCompactPack(fileContent))
							compact = true;
						var preprocessed = Preprocess(fileInfo.Name, fileContent);
						contents.Add(Tuple.Create(fileInfo, preprocessed.Item2, preprocessed.Item3));
						usings.UnionWith(preprocessed.Item1);
					}
				}
				Console.Out.WriteLine($"Compaction: {(compact ? "on" : "off")}");

				result.AppendLine();
				foreach (var u in usings.Where(x => !string.IsNullOrEmpty(x)))
				{
					result.AppendLine(u);
				}

				result.AppendLine();
				result.AppendLine("namespace Game");
				result.AppendLine("{");
				
				foreach (var tuple in contents.OrderBy(t => t.Item3).ThenBy(t => t.Item1.DirectoryName.ToLowerInvariant()))
				{
					Console.Out.WriteLine($"Writing {tuple.Item1.Name}");
					result.AppendLine(tuple.Item2);
				}

				result.AppendLine("}");

				
				var reader = new StringReader(result.ToString());

				result = new StringBuilder();

				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line))
						result.AppendLine(compact ? line.Trim() : line);
				}

				File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "main.cs"), result.ToString());
				Console.Out.WriteLine("Result was saved to disk");

				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return -1;
			}
		}

		private static bool IsCompactPack(string fileContent)
		{
			var options = GetOptions(fileContent);
			return options.IndexOf("compact", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string GetOptions(string fileContent)
		{
			var optionsRegex = new Regex(@"//\s*packOptions\s*:\s*(?<options>.+)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
			var optionsMatch = optionsRegex.Match(fileContent);
			var options = optionsMatch.Groups["options"].Value;
			return options.Trim();
		}

		private static Tuple<string[], string, int> Preprocess(string fileName, string fileContent)
		{
			fileContent = ReplaceHexConstants(ReplaceBinConstants(fileContent));
			var orderRegex = new Regex(@"//\s*pack\s*:\s*(?<order>\d+)", RegexOptions.Singleline | RegexOptions.Compiled);
			var orderMatch = orderRegex.Match(fileContent);
			var order = int.MaxValue;
			if (orderMatch.Success)
				order = int.Parse(orderMatch.Groups["order"].Value);
			var regex = new Regex(@"^(?<using>.*?)(?<header>namespace.*?\n{)", RegexOptions.Singleline | RegexOptions.Compiled);
			var match = regex.Match(fileContent);
			if (!match.Success)
				throw new InvalidOperationException($"Couldn't preprocess file {fileName}");

			var usingsString = match.Groups["using"].Value.Trim();
			var usings = usingsString.Split('\n').Select(x => x.Trim()).Where(x => !x.StartsWith("using Game")).ToArray();
			var content = fileContent.Substring(match.Groups["header"].Index + match.Groups["header"].Length).TrimEnd().TrimEnd('}').TrimEnd();

			return Tuple.Create(usings, content, order);
		}

		private static string ReplaceBinConstants(string fileContent)
		{
			var binRegex = new Regex(@"0b[01][01_]*[01]", RegexOptions.Singleline | RegexOptions.Compiled);
			return binRegex.Replace(fileContent,
				match =>
				{
					var s = match.Value.Substring(2).Replace("_", "");
					long value = 0;
					foreach (var ch in s)
					{
						value <<= 1;
						if (ch == '1')
							value += 1;
					}
					return value.ToString();
				});
		}

		private static string ReplaceHexConstants(string fileContent)
		{
			var binRegex = new Regex(@"0x[0-9abcdefABCDEF][0-9abcdefABCDEF_]*[0-9abcdefABCDEF]", RegexOptions.Singleline | RegexOptions.Compiled);
			return binRegex.Replace(fileContent, match => match.Value.Replace("_", ""));
		}

		private static bool IsExcluded(FileInfo fileInfo)
		{
			if (fileInfo.Name.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
				return true;
			for (var d = fileInfo.Directory; d != null; d = d.Parent)
				if (d.Name.Equals("obj", StringComparison.OrdinalIgnoreCase)
				    || d.Name.Equals("bin", StringComparison.OrdinalIgnoreCase)
				    || d.Name.Equals(".vs", StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}
	}
}