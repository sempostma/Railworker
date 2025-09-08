using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Railworker.Pages
{
    public partial class TrainPerformanceCalculator : Page
    {
        private const double G = 9.81; // m/s^2

        public TrainPerformanceCalculator()
        {
            InitializeComponent();
            SolveForCombo.SelectedIndex = 0; // default solve for weight
        }

        private void SolveForCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEnabledStates();
        }

        private void UpdateEnabledStates()
        {
            string solveFor = ((ComboBoxItem)SolveForCombo.SelectedItem).Content.ToString();

            WeightText.IsEnabled = solveFor != "Weight";
            TractiveEffortText.IsEnabled = solveFor != "Tractive Effort";
            SpeedText.IsEnabled = solveFor != "Speed";
            GradeText.IsEnabled = solveFor != "Grade";
            PowerText.IsEnabled = solveFor != "Power";
        }

        private bool TryParse(TextBox box, out double value)
        {
            value = 0;
            if (!box.IsEnabled) return true; // ignore disabled fields
            if (string.IsNullOrWhiteSpace(box.Text)) return false;
            return double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            WarningText.Text = string.Empty;
            ResultValueText.Text = "-";
            WorkingText.Text = string.Empty;

            string solveFor = ((ComboBoxItem)SolveForCombo.SelectedItem).Content.ToString();

            if (!TryParse(WeightText, out double weightTons)) { WarningText.Text = "Invalid weight"; return; }
            if (!TryParse(TractiveEffortText, out double tractiveEffortkN)) { WarningText.Text = "Invalid tractive effort"; return; }
            if (!TryParse(SpeedText, out double speedKmH)) { WarningText.Text = "Invalid speed"; return; }
            if (!TryParse(GradeText, out double gradePercent)) { WarningText.Text = "Invalid grade"; return; }
            if (!TryParse(PowerText, out double powerkW)) { WarningText.Text = "Invalid power"; return; }

            try
            {
                switch (solveFor)
                {
                    case "Weight":
                        SolveForWeight(tractiveEffortkN, gradePercent);
                        break;
                    case "Tractive Effort":
                        SolveForTractiveEffort(weightTons, gradePercent);
                        break;
                    case "Speed":
                        SolveForSpeed(powerkW, tractiveEffortkN);
                        break;
                    case "Grade":
                        SolveForGrade(weightTons, tractiveEffortkN);
                        break;
                    case "Power":
                        SolveForPower(tractiveEffortkN, speedKmH);
                        break;
                }
            }
            catch (Exception ex)
            {
                WarningText.Text = ex.Message;
            }
        }

        private void SolveForWeight(double tractiveEffortkN, double gradePercent)
        {
            if (tractiveEffortkN <= 0 || gradePercent <= 0) throw new Exception("Tractive effort and grade must be > 0");
            double F = tractiveEffortkN * 1000; // N
            double gradeFraction = gradePercent / 100.0;
            double massKg = F / (G * gradeFraction);
            double massTons = massKg / 1000.0;
            ResultValueText.Text = massTons.ToString("F2", CultureInfo.InvariantCulture) + " t";
            WorkingText.Text = $"m = F / (g * gradeFraction) = {F:F2} / ({G} * {gradeFraction:F4}) = {massKg:F2} kg = {massTons:F2} t";
        }

        private void SolveForTractiveEffort(double weightTons, double gradePercent)
        {
            if (weightTons <= 0 || gradePercent <= 0) throw new Exception("Weight and grade must be > 0");
            double massKg = weightTons * 1000;
            double gradeFraction = gradePercent / 100.0;
            double F = massKg * G * gradeFraction; // N
            double FkN = F / 1000.0;
            ResultValueText.Text = FkN.ToString("F2", CultureInfo.InvariantCulture) + " kN";
            WorkingText.Text = $"F = m * g * gradeFraction = {massKg:F2} * {G} * {gradeFraction:F4} = {F:F2} N = {FkN:F2} kN";
        }

        private void SolveForSpeed(double powerkW, double tractiveEffortkN)
        {
            if (powerkW <= 0 || tractiveEffortkN <= 0) throw new Exception("Power and tractive effort must be > 0");
            double P = powerkW * 1000; // W
            double F = tractiveEffortkN * 1000; // N
            double v = P / F; // m/s
            double vKmH = v * 3.6;
            ResultValueText.Text = vKmH.ToString("F2", CultureInfo.InvariantCulture) + " km/h";
            WorkingText.Text = $"v = P / F = {P:F2} / {F:F2} = {v:F2} m/s = {vKmH:F2} km/h";
        }

        private void SolveForGrade(double weightTons, double tractiveEffortkN)
        {
            if (weightTons <= 0 || tractiveEffortkN <= 0) throw new Exception("Weight and tractive effort must be > 0");
            double massKg = weightTons * 1000;
            double F = tractiveEffortkN * 1000; // N
            double gradeFraction = F / (massKg * G);
            double gradePercent = gradeFraction * 100.0;
            ResultValueText.Text = gradePercent.ToString("F2", CultureInfo.InvariantCulture) + " %";
            WorkingText.Text = $"grade% = (F / (m * g)) * 100 = ({F:F2} / ({massKg:F2} * {G})) * 100 = {gradePercent:F2} %";
        }

        private void SolveForPower(double tractiveEffortkN, double speedKmH)
        {
            if (tractiveEffortkN <= 0 || speedKmH <= 0) throw new Exception("Tractive effort and speed must be > 0");
            double F = tractiveEffortkN * 1000; // N
            double v = speedKmH / 3.6; // m/s
            double P = F * v; // W
            double PkW = P / 1000.0;
            ResultValueText.Text = PkW.ToString("F2", CultureInfo.InvariantCulture) + " kW";
            WorkingText.Text = $"P = F * v = {F:F2} * {v:F2} = {P:F2} W = {PkW:F2} kW";
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            WeightText.Text = string.Empty;
            TractiveEffortText.Text = string.Empty;
            SpeedText.Text = string.Empty;
            GradeText.Text = string.Empty;
            PowerText.Text = string.Empty;
            ResultValueText.Text = "-";
            WorkingText.Text = string.Empty;
            WarningText.Text = string.Empty;
        }
    }
}
