using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace p2simulacion_montecarlo {
	class Program {

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

		public static object Eval(string sCSCode) {

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
				Console.WriteLine("ERROR: " + cr.Errors[0].ErrorText + "\nError evaluating cs code");
				return null;
			}

			System.Reflection.Assembly a = cr.CompiledAssembly;
			object o = a.CreateInstance("CSCodeEvaler.CSCodeEvaler");

			Type t = o.GetType();
			MethodInfo mi = t.GetMethod("EvalCode");

			object s = mi.Invoke(o, null);
			return s;
		}

		static void integral(int size, Func<double, double> f, int iter = 1000, bool print = false) {

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

			double[] values = new double[iter];

			for(int ii = 0; ii < iter; ii++) {
				double[] nx = generateRandomSeries(size);
				double[] ny = generateRandomSeries(size);
				double[] nf = new double[size];
				BoolInteger[] cond = new BoolInteger[size];

				for(int i = 0; i < size; i++) {
					int val = (int) (f(nx[i]) * 10000);
					nf[i] = val / 10000;
				}

				for(int i = 0; i < size; i++) {
					cond[i] = new BoolInteger(ny[i] < nf[i]);
				}

				int count = 0;
				foreach(BoolInteger bi in cond) {
					count += bi.GetIntValue();
				}

				if(print) {
					string res = "+-----------------------------------------+\n";
					res += $"|   Iteración {ii}{blanks[4 - (ii + "").Length]}                        |\n";
					res += "+---------+---------+----------+----------+\n";
					res += "|    X    |    Y    |   f(x)   |   Cond   |\n";
					res += "+---------+---------+----------+----------+\n";
					for(int i = 0; i < size; i++) {
						int snx = (nx[i] + "").Length;
						int sny = (ny[i] + "").Length;
						int snf = (nf[i] + "").Length;
						string c = cond[i].GetBoolValue() ? "V" : "F";
						res += $"| {nx[i]}{blanks[8 - snx]}| {ny[i]}{blanks[8 - sny]}| {nf[i]}{blanks[8 - snf]}|   {c}    |\n";
					}
					res += "+---------+---------+----------+----------+\n";
					Console.WriteLine(res);
				}

				values[ii] = count / size;
			}

			double a = 0;
			foreach(double v in values) {
				a += v;
			}

			a /= iter;

			Console.WriteLine("Área = {0}", a);

		}

		static void borracho(int size, int iter = 15, int end = 0, bool print = false) {
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

			BoolInteger[] values = new BoolInteger[iter];

			for(int ii = 0; ii < iter; ii++) {
				double[] numerosr = generateRandomSeries(size);
				int x = 0, y = 0;
				int[] xx = new int[size];
				int[] yy = new int[size];
				for(int i = 0; i < size; i++) {
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
				if(print) {
					string res = "+------------------+-------+-------+\n";
					res += $"| Prueba           |   X   |   Y   |\n";
					res += "+---------+--------+-------+-------+\n";
					res += "| Inicio  |  NPSA  |   0   |   0   |\n";
					for(int i = 0; i < size; i++) {
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
					Console.WriteLine(res);
				}
				values[ii] = new BoolInteger(((x < 0 ? x * -1 : x) + (y < 0 ? y * -1 : y)) == end);
			}

			double p = 0;
			foreach(BoolInteger v in values) {
				p += v.GetIntValue();
			}
			p /= iter;
			int e = (int) (p * 100);
			Console.WriteLine("La probabilidad de éxito es {0}%", e);
		}

		static bool salir = false;

		private static void menu() {
			//double[] numeros = null;
			Console.WriteLine("Escoja la opción deseada:");
			//Console.WriteLine("\ta)\tGenerar serie.");
			//Console.WriteLine("\tb)\tMostar serie.");
			Console.WriteLine("\ta)\tFunción de Integral f(x).");
			Console.WriteLine("\tb)\tBorracho Aleatorio.");
			//Console.WriteLine("\te)\t.");
			//Console.WriteLine("\tf)\tPrueba de Distribución de Poisson.");
			Console.WriteLine("\totro)\tSalir.");
			Console.Write("Opción: ");
			switch(Console.ReadLine()) {
				case "a":
					Console.Write("Introduzca la cantidad de iteraciones: ");
					int size = int.Parse(Console.ReadLine());
					Console.WriteLine("Introduzca su función:");
					Console.WriteLine("potencia como: a^b -> pow(a, b)");
					Console.WriteLine("raíz como: √a -> sqrt(a)");
					Console.Write("f(x) = ");
					string f = Console.ReadLine();
					Func<double, double> fx = (Func<double, double>) Eval("(x) => " + f + ";");
					integral(size, fx);
					break;
				case "b":
					Console.Write("Introduzca la cantidad de iteraciones: ");
					size = int.Parse(Console.ReadLine());
					//Console.Write("Introduzca la distancia final (si deja en blanco se utiliza 0): ");
					//string es = Console.ReadLine();
					//int end = es != "" ? int.Parse(es) : 0;
					//borracho(size, iter:, end:, print);
					borracho(size);
					break;
				case "c":
					
					break;
				case "d":
					
					break;
				case "e":
					
					break;
				case "f":
					
					break;
				case "g":


					break;
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
					Func<double, double> e = (Func<double, double>) Eval("(x) => " + f + ";");
					Console.WriteLine(e(3));
					break;
				default:
					salir = true;
					break;
			}
			Console.WriteLine("\n");
		}

		static void Main(string[] args) {
			Console.WriteLine("***************************");
			Console.WriteLine("*       Bienvenido        *");
			Console.WriteLine("* Simulación - Práctica 2 *");
			Console.WriteLine("* Daniel Mendoza          *");
			Console.WriteLine("***************************\n");
			while(!salir) {
				menu();
			}
			Console.WriteLine("Adios!");
			System.Threading.Thread.Sleep(700);
		}
	}
}
