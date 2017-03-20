using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RandomAccessMachineEmulator
{
    public partial class MainForm : Form
    {
        int[] registers = new int[0];
        int cursor;
        int instructions;
        int currentLine;

        int nextLine = -1;
        int step = 1;

        public MainForm()
        {
            InitializeComponent();
            codeBox.Focus();

            addNewInputField();
        }

        private void addNewInputField()
        {
            TextBox tb = new TextBox();

            tb.TextChanged += Tb_TextChanged;

            inputPanel.Controls.Add(tb);
        }
        private void Tb_TextChanged(object sender, EventArgs e)
        {
            if (sender == inputPanel.Controls[inputPanel.Controls.Count - 1])
            {
                addNewInputField();
            }
        }

        private async void okButton_Click(object sender, EventArgs e)
        {
            cursor = 0;
            instructions = 0;
            currentLine = 0;

            ClearRegisters();

            outputList.Items.Clear();
            consoleBox.Items.Clear();
            consoleBox.Items.Add(new Log(-1, LogType.Succes, "Program started"));
            registers = new int[8];
            for (int i = 0; i < codeBox.Lines.Length;)
            {
                i = PerformLine(i);
                if (i < 0) return;
                currentLine = i;
            }
            nextLine = -1;

            ShowRegisters();
        }

        private void stepButton_Click(object sender, EventArgs e)
        {
            if (nextLine < 0)
            {
                cursor = 0;
                instructions = 0;
                currentLine = 0;
                step = 0;

                ClearRegisters();

                outputList.Items.Clear();
                consoleBox.Items.Clear();

                consoleBox.Items.Add(new Log(-1, LogType.Succes, "Program started"));
                nextLine = 0;
            }

            nextLine = PerformLine(nextLine);
            step++;

            if (nextLine >= 0 && nextLine < codeBox.Lines.Length)
            {
                int start = 0;
                for (int i = 0; i < nextLine; i++) start += codeBox.Lines[i].Length;
                codeBox.Select(start, codeBox.Lines[nextLine].Length);
            }

            ShowRegisters();
        }

        private int? GetInput(int pos)
        {
            if (pos < inputPanel.Controls.Count)
            {
                if (int.TryParse(inputPanel.Controls[pos].Text, out int input))
                {
                    return input;
                }
            }
            return null;
        }

        private void ShowRegisters()
        {
            registerPanel.Controls.Clear();

            for (int i = 0; i < registers.Length; i++)
            {
                LabeledBox lb = new LabeledBox(i.ToString() + ":", registers[i].ToString());

                registerPanel.Controls.Add(lb);
            }
        }

        private void ClearRegisters()
        {
            for (int i = 0;i < registers.Length; i++)
            {
                registers[i] = 0;
            }
        }

        private int PerformLine(int line)
        {
            string text = codeBox.Lines[line];

            string[] words = text.Split(' ');
            int num = 0;
            if (words.Length > 0)
            {
                if (words[num] != "")
                {
                    if (words[num][words[num].Length - 1] == ':')
                    {
                        num++;
                    }
                    else if (words.Length <= num + 1)
                    {
                        if (words.Length >= num && words[num].ToUpper() == "HALT")
                        {
                            consoleBox.Items.Add(new Log(line, LogType.Succes, "Program properly ended at line " + line.ToString() + " after " + instructions.ToString() + " instructions"));
                            return -1;
                        }
                        else return ++line;
                    }
                }
                int? r;
                switch (words[num].ToUpper())
                {
                    case "LOAD":
                        r = GetValueFromRegister(words[num + 1]);
                        SetValueToRegister(0, (int)r);
                        break;
                    case "STORE":
                        r = GetValueFromRegister(words[num + 1]);
                        SetValueToRegister(r, registers[0]);
                        break;
                    case "ADD":
                        r = GetValueFromRegister(words[num + 1]);
                        SetValueToRegister(0, registers[0] + (int)r);
                        break;
                    case "SUB":
                        r = GetValueFromRegister(words[num + 1]);
                        SetValueToRegister(0, registers[0] - (int)r);
                        break;
                    case "MULT":
                        r = GetValueFromRegister(words[num + 1]);
                        SetValueToRegister(0, registers[0] * (int)r);
                        break;
                    case "DIV":
                        r = GetValueFromRegister(words[num + 1]);
                        if (r == 0)
                        {
                            consoleBox.Items.Add(new Log(line, LogType.Error, "Division by zero"));
                            return -2;
                        }
                        SetValueToRegister(0, registers[0] / (int)r);
                        break;
                    case "READ":
                        r = GetValueFromRegister(words[num + 1]);

                        var input = GetInput(cursor);

                        if (input != null)
                        {
                            SetValueToRegister(r, input.Value);
                            consoleBox.Items.Add(new Log(line, LogType.Norm, "Read from input (value " + input + " at position " + cursor + ")"));
                            cursor++;
                        }
                        else
                        {
                            consoleBox.Items.Add(new Log(line, LogType.Error, "Can not read input from position " + cursor + ""));
                            return -2;
                        }

                        break;
                    case "WRITE":
                        r = GetValueFromRegister(words[num + 1]);
                        consoleBox.Items.Add(new Log(line, LogType.Norm, "Write value \"" + r.ToString() + "\" at output " + outputList.Items.Count.ToString() + ""));
                        outputList.Items.Add(r.ToString());
                        break;
                    case "JUMP":
                        return GoToLine(words[num + 1], line);
                    case "JGTZ":
                        if (registers[0] > 0)
                        {
                            return GoToLine(words[num + 1], line);
                        }
                        break;
                    case "JZERO":
                        if (registers[0] == 0)
                        {
                            return GoToLine(words[num + 1], line);
                        }
                        break;
                    default:
                        consoleBox.Items.Add(new Log(line, LogType.Warning, "Unknown command"));
                        break;
                }
            }
            instructions++;
            return ++line;
        }

        private int GoToLine(string word, int current)
        {

            for (int line = 0; line < codeBox.Lines.Length; line++)
            {
                int pos = codeBox.Lines[line].IndexOf(word + ':');
                if (pos == 0 || (pos > 0 && codeBox.Lines[line][pos-1] == ' '))
                {
                    if (line != current) return line;
                    else
                    {
                        consoleBox.Items.Add(new Log(line, LogType.Error, "Infinite loop"));
                        return -1;
                    }
                }
            }
                
            consoleBox.Items.Add(new Log(current, LogType.Error, "Unknown label \"" + word + "\""));
            return -1;
        }

        private int? GetValueFromRegister(string word)
        {
            int r;
            if (word[0] == '=')
            {
                if (int.TryParse(word.Substring(1), out r)) return r;
                else
                {
                    consoleBox.Items.Add(new Log(currentLine, LogType.Error, "Unknown operator \"" + word + "\". Allowed values are numbers possibly preceded by * or =."));
                    return null;
                }
            }
            else if (word[0] == '*')
            {
                if (int.TryParse(word.Substring(1), out r))
                {
                    if (r < registers.Length) return registers[r];
                    else return 0;
                }
                else
                {
                    consoleBox.Items.Add(new Log(currentLine, LogType.Error, "Unknown operator \"" + word + "\". Allowed values are numbers possibly preceded by * or =."));
                    return null;
                }
            }
            else if (int.TryParse(word, out r))
            {
                return r;
            }
            else
            {
                consoleBox.Items.Add(new Log(currentLine, LogType.Error, "Unknown operator \"" + word + "\". Allowed values are numbers possibly preceded by * or =."));
                return null;
            }
        }

        private void SetValueToRegister(int? reg, int value)
        {
            if (reg < registers.Length)
            {
                registers[(int)reg] = value;
            }
            else
            {
                int r = (int)reg;
                int[] neue = new int[r + 1];
                for (int i = 0; i < registers.Length;i++)
                {
                    neue[i] = registers[i];
                }
                registers = neue;
                registers[r] = value;
            }
        } 

        public void SelectLine(int line)
        {
            if (line < codeBox.Lines.Length)
            {
                int start = 0;
                for (int i = 0; i < line; i++) start += codeBox.Lines[i].Length + 1;
                codeBox.Select(start, codeBox.Lines[line].Length);
                codeBox.Focus();
            }
        }

        private void consoleBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            if (e.Index >= 0)
            {
                Log log = consoleBox.Items[e.Index] as Log;
                if (log != null)
                {
                    g.FillRectangle(new SolidBrush(log.BackColor), e.Bounds);
                    g.DrawString(log.Text, e.Font, Brushes.Black, e.Bounds);
                }

                if (e.Index == consoleBox.SelectedIndex) g.DrawRectangle(SystemPens.ActiveBorder, e.Bounds);
            }
        }

        private void consoleBox_DoubleClick(object sender, EventArgs e)
        {
            Log log = consoleBox.SelectedItem as Log;
            if (log != null)
            {
                MainForm form = FindForm() as MainForm;
                if (form != null)
                log.Log_DoubleClick(form);
            }
        }
    }

    public enum LogType { Norm, Warning, Error, Succes}

    public class Log : Label
    {
        int Line;
        public Log(int line, LogType type, string text)
        {
            Text = text;
            Line = line;
            switch (type)
            {
                case LogType.Norm:
                    if (line >= 0)
                    Text = "Line " + line.ToString() + ": " + Text;
                    break;
                case LogType.Warning:
                    BackColor = Color.Yellow;
                    if (line >= 0)
                        Text = "Error ignored at line " + line.ToString() + ": " + Text;
                    else Text = "Warning: " + Text;
                    break;
                case LogType.Error:
                    BackColor = Color.Red;
                    ForeColor = Color.White;
                    if (line >= 0)
                        Text = "Error at line " + line.ToString() + ": " + Text;
                    else Text = "Error: " + Text;
                    break;
                case LogType.Succes:
                    BackColor = Color.Lime;
                    break;
            }
        }

        public override string ToString()
        {
            return Text;
        }

        public static explicit operator Log(string s)
        {
            return new Log(-1, LogType.Norm, s);
        }

        public void Log_DoubleClick(MainForm form)
        {
            if (Line >= 0)
            form.SelectLine(Line);
        }
    }
}
