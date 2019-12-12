using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace p2simulacion_montecarlo {
	class Program {

		static string path;

		internal class BoolInteger {
			private bool value;
			public BoolInteger(int val) {
				value = val == 1;
			}

			public BoolInteger(bool val) {
				value = val;
			}

			public bool GetBoolValue() {
				return value;
			}

			public int GetIntValue() {
				return value ? 1 : 0;
			}

			public static BoolInteger operator *(BoolInteger a, BoolInteger b) {
				return new BoolInteger(a == b);
			}

			public static BoolInteger operator +(BoolInteger a, BoolInteger b) {
				return new BoolInteger(a.value || b.value);
			}

			public override string ToString() {
				return value ? "1" : "0";
			}

		}

		static Random RND = new Random();

		private static int[] generate() {
			var nums = new List<int>();
			for(int a = 0; a < 10; a++) {
				int numa = a;
				for(int b = 0; b < 10; b++) {
					if(b == a)
						continue;
					int numb = numa * 10 + b;
					for(int c = 0; c < 10; c++) {
						if(c == a || c == b)
							continue;
						int numc = numb * 10 + c;
						for(int d = 0; d < 10; d++) {
							if(d == a || d == b || d == c)
								continue;
							//Console.WriteLine(numc * 10 + d);
							nums.Add(numc * 10 + d);
						}
					}
				}
			}
			return nums.ToArray();
		}

		static int[] numeros = generate();

		static bool salir = false;

		static bool print = false;

		static bool save = false;

		private static int RandomNumber() {
			int rnd = RND.Next(numeros.Length);
			//Console.Write("rnd: " + rnd + "; ");
			return numeros[rnd];
		}

		private static string printArray(double[] arr) {
			string res = "\n[\n";
			int l = arr.Length;
			for(int i = 0; i < l - 1; i++) {
				res += "  " + arr[i] + ",\n";
				//res += "\t" +  + ",\n";
			}
			return res + "  " + arr[l - 1] + "\n]\n";
		}

		private static string printArray(int[] arr) {
			string res = "\n[\n";
			int l = arr.Length;
			for(int i = 0; i < l - 1; i++) {
				res += "  " + arr[i] + ",\n";
				//res += "\t" +  + ",\n";
			}
			return res + "  " + arr[l - 1] + "\n]\n";
		}

		private static double[] generateRandomSeries(int size) {
			double[] res = new double[size];
			for(int i = 0; i < size; i++) {
				double n = RandomNumber();
				n = Math.Pow(n, 2);
				string ns = n + "";
				if(ns.Length == 8) {
					ns = "0." + ns.Substring(2, 4);
				} else {
					ns = "0." + ns.Substring(1, 4);
				}
				n = double.Parse(ns);
				res[i] = n;
			}
			return res;
		}

		public static object Eval(string sCSCode, string org) {

			CSharpCodeProvider c = new CSharpCodeProvider();
			ICodeCompiler icc = c.CreateCompiler();
			CompilerParameters cp = new CompilerParameters();

			cp.ReferencedAssemblies.Add("system.dll");
			cp.ReferencedAssemblies.Add("system.xml.dll");
			cp.ReferencedAssemblies.Add("system.data.dll");
			cp.ReferencedAssemblies.Add("system.windows.forms.dll");
			cp.ReferencedAssemblies.Add("system.drawing.dll");

			cp.CompilerOptions = "/t:library";
			cp.GenerateInMemory = true;

			StringBuilder sb = new StringBuilder("");
			sb.Append("using System;\n");
			sb.Append("using System.Xml;\n");
			sb.Append("using System.Data;\n");
			sb.Append("using System.Data.SqlClient;\n");
			sb.Append("using System.Windows.Forms;\n");
			sb.Append("using System.Drawing;\n");

			sb.Append("namespace CSCodeEvaler{ \n");
			sb.Append("public class CSCodeEvaler{ \n");
			sb.Append("public object EvalCode(){ \n");
			sb.Append("Func<double, double> func = " + sCSCode + "\n");
			//sb.Append("return " + sCSCode + "; \n");
			sb.Append("return func; \n");
			sb.Append("} \n");
			sb.Append("} \n");
			sb.Append("}\n");

			CompilerResults cr = icc.CompileAssemblyFromSource(cp, sb.ToString());
			if(cr.Errors.Count > 0) {
				//Console.WriteLine("ERROR: " + cr.Errors[0].ErrorText + "\nError evaluating cs code");
				Console.WriteLine("ERROR !\n\tFunción *{0}* incorrecta", org);
				return null;
			}

			System.Reflection.Assembly a = cr.CompiledAssembly;
			object o = a.CreateInstance("CSCodeEvaler.CSCodeEvaler");

			Type t = o.GetType();
			MethodInfo mi = t.GetMethod("EvalCode");

			object s = mi.Invoke(o, null);
			return s;
		}

		static void integral(Func<double, double> f, int iteraciones = 500, int simulaciones = 1000) {

			string[] blanks = {
				"",
				" ",
				"  ",
				"   ",
				"    ",
				"     ",
				"      ",
				"       ",
				"        ",
				"         "
			};

			string[] toPrint = new string[simulaciones];

			double[] values = new double[simulaciones];

			for(int ii = 0; ii < simulaciones; ii++) {
				double[] nx = generateRandomSeries(iteraciones);
				double[] ny = generateRandomSeries(iteraciones);
				double[] nf = new double[iteraciones];
				BoolInteger[] cond = new BoolInteger[iteraciones];

				for(int i = 0; i < iteraciones; i++) {
					//double fv = f(nx[i]);
					//Console.WriteLine(fv);
					//int val = (int) (fv * 10000);
					int val = (int) (f(nx[i]) * 10000);
					//Console.WriteLine(val);
					//Console.WriteLine(val / 10000);
					nf[i] = (double) val / 10000;
				}

				for(int i = 0; i < iteraciones; i++) {
					cond[i] = new BoolInteger(ny[i] < nf[i]);
				}

				double count = 0;
				foreach(BoolInteger bi in cond) {
					count += bi.GetIntValue();
				}

				if(print || save) {
					string res = "+-----------------------------------------+\n";
					res += $"|   Iteración {ii}{blanks[4 - (ii + "").Length]}                        |\n";
					res += "+---------+---------+----------+----------+\n";
					res += "|    X    |    Y    |   f(x)   |   Cond   |\n";
					res += "+---------+---------+----------+----------+\n";
					for(int i = 0; i < iteraciones; i++) {
						int snx = (nx[i] + "").Length;
						int sny = (ny[i] + "").Length;
						int snf = (nf[i] + "").Length;
						string c = cond[i].GetBoolValue() ? "V" : "F";
						res += $"| {nx[i]}{blanks[8 - snx]}| {ny[i]}{blanks[8 - sny]}| {nf[i]}{blanks[8 - snf]}|   {c}    |\n";
					}
					res += "+---------+---------+----------+----------+\n";
					if(print)
						Console.WriteLine(res);
					toPrint[ii] = res;
				}

				values[ii] = count / iteraciones;
				//Console.WriteLine(count);
				//Console.WriteLine(count / iteraciones);
				//Console.WriteLine(printArray(nx));
				//Console.WriteLine(printArray(ny));
				//Console.WriteLine(printArray(nf));
			}

			if(save)
				System.IO.File.WriteAllLines($@"{path}\integral.txt", toPrint);

			double a = 0.0;
			foreach(double v in values) {
				a += v;
			}

			a /= simulaciones;

			Console.WriteLine("Área = {0}", a);

		}

		static void borracho(int iteraciones = 15, int simulaciones = 500, int end = 2) {
			string[] blanks = {
				"",
				" ",
				"  ",
				"   ",
				"    ",
				"     ",
				"      ",
				"       ",
				"        ",
				"         "
			};

			BoolInteger[] values = new BoolInteger[simulaciones];

			string[] toPrint = new string[simulaciones];

			for(int ii = 0; ii < simulaciones; ii++) {
				double[] numerosr = generateRandomSeries(iteraciones);
				int x = 0, y = 0;
				int[] xx = new int[iteraciones];
				int[] yy = new int[iteraciones];
				for(int i = 0; i < iteraciones; i++) {
					double r = numerosr[i];
					if(r < 0.25) {
						x += 1;
					} else if(r < 0.5) {
						x -= 1;
					} else if(r < 0.75) {
						y += 1;
					} else {
						y -= 1;
					}
					xx[i] = x;
					yy[i] = y;
				}
				if(print || save) {
					string res = "+------------------+-------+-------+\n";
					res += $"| Prueba   {(ii + 1)}{blanks[3 - ((ii + 1) + "").Length]}     |   X   |   Y   |\n";
					res += "+---------+--------+-------+-------+\n";
					res += "| Inicio  |  NPSA  |   0   |   0   |\n";
					for(int i = 0; i < iteraciones; i++) {
						//{i + 1}{blanks[3 - ((i + 1) + "").Length]}
						string sy, sx;
						int ny = yy[i], nx = xx[i];
						if(ny < 0) {
							sy = "-";
							ny *= -1;
						} else {
							sy = " ";
						}
						if(xx[i] < 0) {
							sx = "-";
							nx *= -1;
						} else {
							sx = " ";
						}

						res += $"|   {i + 1}{blanks[3 - ((i + 1) + "").Length]}   | {numerosr[i]}{blanks[6 - (numerosr[i] + "").Length]} |  {sx}{nx}   |  {sy}{ny}   |\n";
					}
					res += "+---------+--------+-------+-------+\n";
					if(print)
						Console.WriteLine(res);
					toPrint[ii] = res;
				}
				//Console.WriteLine("i: {0}; {1} {2} {3} {4}", ii, x, y, end, Math.Abs(x) + Math.Abs(y) == end);
				values[ii] = new BoolInteger(Math.Abs(x) + Math.Abs(y) == end);
			}
			if(save)
				System.IO.File.WriteAllLines($@"{path}\borracho.txt", toPrint);


			double p = 0;
			foreach(BoolInteger v in values) {
				p += (double) v.GetIntValue();
				//Console.WriteLine(v.GetIntValue());
			}
			p /= simulaciones;
			int e = (int) (p * 100);
			//Console.WriteLine(p);
			//Console.WriteLine(e);
			Console.WriteLine("La probabilidad de éxito es {0}%", e);
		}

		private static void menu() {
			Console.WriteLine("Escoja la opción deseada:");
			Console.WriteLine("\ta)\tFunción de Integral f(x).");
			Console.WriteLine("\tb)\tBorracho Aleatorio.");
			Console.WriteLine("\tc)\tActivar Impresión de Simulaciones.");
			Console.WriteLine("\td)\tActivar Guardado de Simulaciones.");
			Console.WriteLine("\totro)\tSalir.");
			Console.Write("Opción: ");
			switch(Console.ReadLine()) {
				case "a":
					Console.Write("Introduzca la cantidad de simulaciones (1000 por defecto): ");
					string s = Console.ReadLine();
					Console.WriteLine("Introduzca su función:");
					Console.WriteLine("potencia a^b -> pow(a, b)");
					Console.WriteLine("raíz √a -> sqrt(a)");
					Console.Write("f(x) = ");
					string f = Console.ReadLine();
					string ff = f;
					f = f.Replace("pow", "Math.Pow");
					f = f.Replace("sqrt", "Math.Sqrt");
					Func<double, double> fx = (Func<double, double>) Eval($"(x) => {f};", ff);
					if(s != "") {
						int size = int.Parse(s);
						integral(simulaciones: size, f: fx);
					} else {
						integral(f: fx);
					}
					break;
				case "b":
					Console.Write("Introduzca la cantidad de simulaciones (500 por defecto): ");
					s = Console.ReadLine();
					Console.Write("Introduzca la distancia destino (2 por defecto): ");
					string ss = Console.ReadLine();
					if(s + ss == "") {
						borracho();
					} else if(s == "") {
						int end = int.Parse(ss);
						borracho(end: end);
					} else if(ss == "") {
						int size = int.Parse(s);
						borracho(simulaciones: size);
					} else {
						int size = int.Parse(s);
						int end = int.Parse(ss);
						borracho(simulaciones: size, end: end);
					}
					break;
				case "c":
					Console.Write("Activar impresiones? (s/n): ");
					string r = Console.ReadLine();
					if(r == "s") {
						print = true;
					} else {
						print = false;
					}
					break;
				case "d":
					Console.Write("Activar guardado? (s/n): ");
					r = Console.ReadLine();
					if(r == "s") {
						Console.Write(
							"{0} {1}: ",
							"Introduzca la ruta donde desea guardar el archivo",
							"(por defecto será utilizada la ruta de ejecución)"
						);
						string p = Console.ReadLine();
						if(p != "") {
							path = p;
						} else {
							path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
						}
						save = true;
					} else {
						save = false;
					}
					break;
				//case "e":
				//	break;
				//case "f":
				//	break;
				//case "g":
				//	break;
				case "arc":
					string ai = Console.ReadLine();
					string[] nn = ai.Split(',');
					//numerosr = new double[nn.Length];
					for(int i = 0; i < nn.Length; i++) {
						double num = double.Parse(nn[i]);
						//numerosr[i] = num;
					}
					break;
				case "eval":
					f = Console.ReadLine();
					char[] separators = new char[] { '+', '-', '*', '/' };
					Func<double, double> e = (Func<double, double>) Eval($"(x) => {f};", f);
					Console.WriteLine(e(3));
					break;
				default:
					salir = true;
					break;
			}
			Console.WriteLine("\n");
		}

		static void Main(string[] args) {
			Console.WriteLine("****************************");
			Console.WriteLine("*        Bienvenido        *");
			Console.WriteLine("* Simulación - Práctica 2  *");
			Console.WriteLine("* Simulación de Montecarlo *");
			Console.WriteLine("* Daniel Mendoza           *");
			Console.WriteLine("****************************\n");
			path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			//Console.WriteLine(path);
			while(!salir) {
				menu();
			}
			Console.WriteLine("Adios!");
			System.Threading.Thread.Sleep(700);
		}
	}
}
