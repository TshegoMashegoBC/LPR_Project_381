using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LPR381Solver.Core;
using LPR381Solver.Algorithms;
using LPR381Solver.IO;

namespace LPR381Solver
{
    public class MainForm : Form
    {
        private ComboBox cmbAlgorithm = null!;
        private ComboBox cmbObjType = null!;
        private NumericUpDown numVars = null!;
        private NumericUpDown numConstraints = null!;
        private ComboBox cmbGlobalSign = null!;
        
        private Panel pnlObjCoeffs = null!;
        private Panel pnlConstraints = null!;
        
        private List<TextBox> objTextBoxes = new List<TextBox>();
        private List<List<TextBox>> constraintCoeffBoxes = new List<List<TextBox>>();
        private List<ComboBox> constraintRelBoxes = new List<ComboBox>();
        private List<TextBox> constraintRhsBoxes = new List<TextBox>();

        private Label lblStatus = null!;
        private Label lblObjVal = null!;
        private RichTextBox txtVarValues = null!;
        private Button btnSolve = null!;
        private Button btnOpenTxt = null!;

        private SolveResult? lastResult;

        public MainForm()
        {
            Text = "LPR381 Operations Research Solver - Belgium Campus";
            Size = new Size(1250, 800);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.WhiteSmoke;

            SetupUI();
            RebuildDynamicInputs();
        }

        private void SetupUI()
        {
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.WhiteSmoke
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 550F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // ==================== LEFT PANEL (INPUTS) ====================
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.White
            };

            int y = 10;

            // 1. Algorithm Selection
            leftPanel.Controls.Add(new Label { Text = "1. Select Algorithm:", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            y += 25;
            cmbAlgorithm = new ComboBox { Location = new Point(15, y), Width = 490, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAlgorithm.Items.AddRange(new string[] { 
                "Primal Simplex (Continuous Relaxation)", 
                "Dual Simplex", 
                "Branch & Bound Simplex", 
                "Cutting Plane Algorithm", 
                "Branch & Bound Knapsack", 
                "Non-Linear Gradient Descent" 
            });
            cmbAlgorithm.SelectedIndex = 0;
            leftPanel.Controls.Add(cmbAlgorithm);
            y += 45;

            // 2. Dimensions & Objective Type
            leftPanel.Controls.Add(new Label { Text = "2. Model Structure & Dimensions:", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            y += 25;

            leftPanel.Controls.Add(new Label { Text = "Type:", Location = new Point(15, y + 3), AutoSize = true });
            cmbObjType = new ComboBox { Location = new Point(60, y), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbObjType.Items.AddRange(new string[] { "Max", "Min" });
            cmbObjType.SelectedIndex = 0;
            leftPanel.Controls.Add(cmbObjType);

            leftPanel.Controls.Add(new Label { Text = "Variables (n):", Location = new Point(170, y + 3), AutoSize = true });
            numVars = new NumericUpDown { Location = new Point(260, y), Width = 60, Minimum = 1, Maximum = 10, Value = 2 };
            numVars.ValueChanged += (s, e) => RebuildDynamicInputs();
            leftPanel.Controls.Add(numVars);

            leftPanel.Controls.Add(new Label { Text = "Constraints (m):", Location = new Point(340, y + 3), AutoSize = true });
            numConstraints = new NumericUpDown { Location = new Point(445, y), Width = 60, Minimum = 1, Maximum = 10, Value = 2 };
            numConstraints.ValueChanged += (s, e) => RebuildDynamicInputs();
            leftPanel.Controls.Add(numConstraints);
            y += 50;

            // 3. Objective Coefficients Dynamic Container
            leftPanel.Controls.Add(new Label { Text = "3. Objective Coefficients (Z):", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            y += 25;
            pnlObjCoeffs = new Panel { Location = new Point(15, y), Width = 490, Height = 45, AutoSize = true };
            leftPanel.Controls.Add(pnlObjCoeffs);
            y += 55;

            // 4. Constraints Dynamic Container
            leftPanel.Controls.Add(new Label { Text = "4. Constraints (Coefficients, Relation, RHS):", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            y += 25;
            pnlConstraints = new Panel { Location = new Point(15, y), Width = 490, Height = 150, AutoSize = true };
            leftPanel.Controls.Add(pnlConstraints);
            y += 165;

            // 5. Global Sign Restriction Dropdown
            leftPanel.Controls.Add(new Label { Text = "5. Global Sign Restrictions (Applies to all x):", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            y += 25;
            cmbGlobalSign = new ComboBox { Location = new Point(15, y), Width = 490, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGlobalSign.Items.AddRange(new string[] { 
                "Integer (int >= 0)", 
                "Binary (bin: 0 or 1)", 
                "Positive (>= 0)", 
                "Negative (<= 0)", 
                "Unrestricted (urs)" 
            });
            cmbGlobalSign.SelectedIndex = 0;
            leftPanel.Controls.Add(cmbGlobalSign);
            y += 65;

            // Solve Button
            btnSolve = new Button { Text = "RUN SOLVE", Location = new Point(15, y), Width = 490, Height = 50, BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnSolve.Click += BtnSolve_Click;
            leftPanel.Controls.Add(btnSolve);

            // ==================== RIGHT PANEL (RESULTS SUMMARY) ====================
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };

            Label lblRightHeader = new Label { Text = "Optimal Solution Summary", Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };
            rightPanel.Controls.Add(lblRightHeader);

            lblStatus = new Label { Text = "Status: Awaiting Execution", Font = new Font("Segoe UI", 11, FontStyle.Italic), Dock = DockStyle.Top, Height = 30, ForeColor = Color.Gray };
            rightPanel.Controls.Add(lblStatus);

            lblObjVal = new Label { Text = "Objective Value (Z): —", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 35, ForeColor = Color.DarkGreen };
            rightPanel.Controls.Add(lblObjVal);

            Label lblVarHeader = new Label { Text = "Optimal Variable Values:", Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            rightPanel.Controls.Add(lblVarHeader);

            txtVarValues = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            rightPanel.Controls.Add(txtVarValues);

            btnOpenTxt = new Button 
            { 
                Text = "OPEN output.txt (View All Iterations)", 
                Dock = DockStyle.Bottom, 
                Height = 50, 
                BackColor = Color.DarkGreen, 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Enabled = false
            };
            btnOpenTxt.Click += (s, e) => {
                if (File.Exists("output.txt"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("output.txt") { UseShellExecute = true });
                }
            };
            rightPanel.Controls.Add(btnOpenTxt);

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);
            Controls.Add(mainLayout);
        }

        private void RebuildDynamicInputs()
        {
            int varCount = (int)numVars.Value;
            int constraintCount = (int)numConstraints.Value;

            // --- Objective Coefficients ---
            pnlObjCoeffs.Controls.Clear();
            objTextBoxes.Clear();
            int xPos = 0;
            for (int i = 0; i < varCount; i++)
            {
                var txt = new TextBox { Width = 50, Text = "0", TextAlign = HorizontalAlignment.Center, Location = new Point(xPos, 5) };
                var lbl = new Label { Text = $"x{i + 1}" + (i < varCount - 1 ? " +" : ""), Location = new Point(xPos + 52, 7), AutoSize = true };
                objTextBoxes.Add(txt);
                pnlObjCoeffs.Controls.Add(txt);
                pnlObjCoeffs.Controls.Add(lbl);
                xPos += 90;
            }

            // --- Constraints ---
            pnlConstraints.Controls.Clear();
            constraintCoeffBoxes.Clear();
            constraintRelBoxes.Clear();
            constraintRhsBoxes.Clear();

            int cY = 5;
            for (int r = 0; r < constraintCount; r++)
            {
                var rowBoxes = new List<TextBox>();
                int cX = 0;
                for (int i = 0; i < varCount; i++)
                {
                    var txt = new TextBox { Width = 45, Text = "0", TextAlign = HorizontalAlignment.Center, Location = new Point(cX, cY) };
                    var lbl = new Label { Text = $"x{i + 1}" + (i < varCount - 1 ? "+" : ""), Location = new Point(cX + 47, cY + 3), AutoSize = true };
                    rowBoxes.Add(txt);
                    pnlConstraints.Controls.Add(txt);
                    pnlConstraints.Controls.Add(lbl);
                    cX += 75;
                }
                constraintCoeffBoxes.Add(rowBoxes);

                var relCmb = new ComboBox { Location = new Point(cX, cY - 1), Width = 55, DropDownStyle = ComboBoxStyle.DropDownList };
                relCmb.Items.AddRange(new string[] { "<=", ">=", "=" });
                relCmb.SelectedIndex = 0;
                constraintRelBoxes.Add(relCmb);
                pnlConstraints.Controls.Add(relCmb);
                cX += 65;

                var rhsTxt = new TextBox { Width = 55, Text = "0", TextAlign = HorizontalAlignment.Center, Location = new Point(cX, cY) };
                constraintRhsBoxes.Add(rhsTxt);
                pnlConstraints.Controls.Add(rhsTxt);

                cY += 35;
            }
        }

        private void BtnSolve_Click(object? sender, EventArgs e)
        {
            try
            {
                IAlgorithm solver = cmbAlgorithm.SelectedIndex switch
                {
                    0 => new PrimalSimplexAlgorithm(),
                    1 => new DualSimplexAlgorithm(),
                    2 => new BranchAndBoundSimplexAlgorithm(),
                    3 => new CuttingPlaneAlgorithm(),
                    4 => new BranchAndBoundKnapsackAlgorithm(),
                    5 => new NonLinearSolver(),
                    _ => throw new Exception("Please select a valid algorithm.")
                };

                LPModel model;
                int varCount = (int)numVars.Value;

                if (solver is NonLinearSolver)
                {
                    model = new LPModel(ObjectiveType.Max, new double[] { 1 }, new List<Constraint> { new Constraint(new double[] { 1 }, Relation.Equal, 1) }, new[] { SignRestriction.Positive });
                }
                else
                {
                    var objType = cmbObjType.SelectedIndex == 0 ? ObjectiveType.Max : ObjectiveType.Min;
                    var objCoeffs = objTextBoxes.Select(t => double.Parse(t.Text)).ToArray();

                    var constraints = new List<Constraint>();
                    for (int r = 0; r < constraintCoeffBoxes.Count; r++)
                    {
                        var coeffs = constraintCoeffBoxes[r].Select(t => double.Parse(t.Text)).ToArray();
                        string relStr = constraintRelBoxes[r].SelectedItem?.ToString() ?? "<=";
                        Relation rel = relStr == ">=" ? Relation.GreaterOrEqual : (relStr == "=" ? Relation.Equal : Relation.LessOrEqual);
                        double rhs = double.Parse(constraintRhsBoxes[r].Text);
                        constraints.Add(new Constraint(coeffs, rel, rhs));
                    }

                    SignRestriction globalSign = cmbGlobalSign.SelectedIndex switch
                    {
                        0 => SignRestriction.Integer,
                        1 => SignRestriction.Binary,
                        2 => SignRestriction.Positive,
                        3 => SignRestriction.Negative,
                        4 => SignRestriction.Unrestricted,
                        _ => SignRestriction.Integer
                    };

                    var signReqs = Enumerable.Repeat(globalSign, varCount).ToArray();
                    model = new LPModel(objType, objCoeffs, constraints, signReqs);
                }

                lastResult = solver.Solve(model);
                OutputWriter.WriteResult("output.txt", lastResult);

                lblStatus.Text = $"Status: {lastResult.Status}";
                if (lastResult.Status == SolveStatus.Optimal)
                {
                    lblStatus.ForeColor = Color.DarkGreen;
                    lblObjVal.Text = $"Objective Value (Z): {lastResult.ObjectiveValue}";
                    
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < lastResult.VariableValues.Length; i++)
                    {
                        sb.AppendLine($"  x{i + 1} = {lastResult.VariableValues[i]}");
                    }
                    txtVarValues.Text = sb.ToString();
                }
                else
                {
                    lblStatus.ForeColor = Color.Red;
                    lblObjVal.Text = "Objective Value (Z): Infeasible / Unbounded";
                    txtVarValues.Text = $"Execution finished with status: {lastResult.Status}";
                }

                btnOpenTxt.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Validation Error: {ex.Message}", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}