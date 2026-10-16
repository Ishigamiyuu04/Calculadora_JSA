using System;
using System.Globalization;
using Microsoft.Maui.Controls;
namespace Calculadora_JSA
{
    public partial class MainPage : ContentPage
    {
        // Guarda la operacion completa.
        // Ejemplo: 12+5*3-4
        private string _expression = string.Empty;

        // Guarda el numero que se esta escribiendo.
        private string _currentEntry = "0";

        // Indica si se va a comenzar a escribir otro numero.
        private bool _startNewEntry = true;

        // Indica si ya se mostro un resultado.
        private bool _hasResult = false;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnNumberClicked(object sender, EventArgs e)
        {
            if (sender is not Button button)
                return;

            string pressed = button.Text;

            // Solo aceptar digitos o el punto decimal.
            if (pressed != "." &&
                (pressed.Length != 1 || !char.IsDigit(pressed[0])))
            {
                return;
            }

            // Si habia un resultado, iniciar una operacion nueva
            // cuando se presiona un numero.
            if (_hasResult)
            {
                _expression = string.Empty;
                _currentEntry = "0";
                _hasResult = false;
                _startNewEntry = true;
            }

            // Manejar el punto decimal.
            if (pressed == ".")
            {
                if (_startNewEntry)
                {
                    _currentEntry = "0.";
                    _expression += "0.";
                    _startNewEntry = false;
                }
                else
                {
                    // Evitar mas de un punto en el mismo numero.
                    if (_currentEntry.Contains("."))
                        return;

                    _currentEntry += ".";
                    _expression += ".";
                }

                UpdateDisplay();
                return;
            }

            // Si se comienza un numero nuevo.
            if (_startNewEntry)
            {
                _currentEntry = pressed;
                _expression += pressed;
                _startNewEntry = false;
            }
            else if (_currentEntry == "0")
            {
                // Evitar numeros como 05 cuando se escribe 0 y luego 5.
                if (_expression.Length > 0 &&
                    _expression.EndsWith("0", StringComparison.Ordinal))
                {
                    _expression =
                        _expression.Substring(0, _expression.Length - 1)
                        + pressed;
                }
                else
                {
                    _expression += pressed;
                }

                _currentEntry = pressed;
            }
            else
            {
                _currentEntry += pressed;
                _expression += pressed;
            }

            UpdateDisplay();
        }

        // ==========================================
        // 2. LIMPIAR TODA LA PANTALLA (C)
        // ==========================================
        private void OnClearClicked(object sender, EventArgs e)
        {
            _expression = string.Empty;
            _currentEntry = "0";
            _startNewEntry = true;
            _hasResult = false;

            UpdateDisplay();
        }

        // ==========================================
        // 3. BORRAR EL ULTIMO CARACTER (⌫)
        // ==========================================
        private void OnDeleteLastClicked(object sender, EventArgs e)
        {
            if (_expression.Length == 0)
            {
                _currentEntry = "0";
                _startNewEntry = true;
                UpdateDisplay();
                return;
            }

            _hasResult = false;

            // Eliminar el ultimo caracter de la expresion.
            _expression = _expression.Substring(
                0, _expression.Length - 1);

            if (_expression.Length == 0)
            {
                _currentEntry = "0";
                _startNewEntry = true;
            }
            else if (EndsWithOperator(_expression))
            {
                _currentEntry = "0";
                _startNewEntry = true;
            }
            else
            {
                // Recuperar el numero que se esta editando.
                int lastOperator = FindLastOperatorIndex(_expression);

                _currentEntry =
                    _expression.Substring(lastOperator + 1);

                if (_currentEntry.Length == 0)
                    _currentEntry = "0";

                _startNewEntry = false;
            }

            UpdateDisplay();
        }

        // ==========================================
        // 4. OPERADORES: +, -, *, / Y %
        // ==========================================
        private void OnOperatorClicked(object sender, EventArgs e)
        {
            if (sender is not Button button)
                return;

            string symbol = button.Text;

            // El porcentaje tiene una logica especial.
            if (symbol == "%")
            {
                HandlePercentage();
                return;
            }

            char op = NormalizeOperator(symbol);

            if (op == '\0')
                return;

            // Permitir comenzar una expresion con un numero negativo.
            if (_expression.Length == 0)
            {
                if (op == '-')
                {
                    _expression = "-";
                    _currentEntry = "0";
                    _startNewEntry = true;
                    UpdateDisplay();
                }

                return;
            }

            // Evitar que la expresion termine con varios operadores.
            if (EndsWithOperator(_expression))
            {
                // No sustituir el signo negativo inicial.
                if (_expression == "-")
                    return;

                // Reemplazar el operador anterior.
                _expression =
                    _expression.Substring(0, _expression.Length - 1)
                    + op;
            }
            else
            {
                // Agregar el operador a la expresion.
                _expression += op;
            }

            _currentEntry = "0";
            _startNewEntry = true;
            _hasResult = false;

            UpdateDisplay();
        }

        // ==========================================
        // 5. CALCULAR PORCENTAJE
        // ==========================================
        private void HandlePercentage()
        {
            if (_expression.Length == 0 ||
                EndsWithOperator(_expression))
            {
                return;
            }

            if (!double.TryParse(
                    _currentEntry,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double currentValue))
            {
                return;
            }

            try
            {
                double percentage;

                int lastOperator = FindLastOperatorIndex(_expression);

                // En suma y resta, el porcentaje se calcula
                // respecto al resultado de la expresion anterior.
                if (lastOperator > 0 &&
                    (_expression[lastOperator] == '+' ||
                     _expression[lastOperator] == '-'))
                {
                    string previousExpression =
                        _expression.Substring(0, lastOperator);

                    double baseValue =
                        EvaluateExpression(previousExpression);

                    percentage = currentValue * baseValue / 100.0;
                }
                else
                {
                    // En un numero aislado, multiplicacion o division,
                    // el porcentaje equivale a dividir entre 100.
                    percentage = currentValue / 100.0;
                }

                if (double.IsNaN(percentage) ||
                    double.IsInfinity(percentage))
                {
                    ShowError();
                    return;
                }

                string formattedPercentage = FormatNumber(percentage);

                // Sustituir el ultimo numero por su porcentaje.
                int entryStart =
                    _expression.Length - _currentEntry.Length;

                if (entryStart < 0)
                    return;

                _expression =
                    _expression.Substring(0, entryStart)
                    + formattedPercentage;

                _currentEntry = formattedPercentage;
                _startNewEntry = false;
                _hasResult = false;

                UpdateDisplay();
            }
            catch (DivideByZeroException)
            {
                ShowError();
            }
            catch (FormatException)
            {
                ShowError();
            }
            catch (OverflowException)
            {
                ShowError();
            }
        }

        // ==========================================
        // 6. BOTON IGUAL
        // ==========================================
        private async void OnCalculateClicked(object sender, EventArgs e)
        {
            // No calcular una expresion vacia o incompleta.
            if (_expression.Length == 0 ||
                EndsWithOperator(_expression))
            {
                return;
            }

            try
            {
                double result = EvaluateExpression(_expression);

                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException();

                // Guardar el resultado para poder seguir operando.
                _currentEntry = FormatNumber(result);
                _expression = _currentEntry;

                _hasResult = true;
                _startNewEntry = true;

                UpdateDisplay();
            }
            catch (DivideByZeroException)
            {
                ShowError();

                await DisplayAlert(
                    "Error",
                    "No se puede dividir entre cero.",
                    "Aceptar");
            }
            catch (FormatException)
            {
                ShowError();

                await DisplayAlert(
                    "Error",
                    "La operacion no es valida.",
                    "Aceptar");
            }
            catch (OverflowException)
            {
                ShowError();

                await DisplayAlert(
                    "Error",
                    "El resultado esta fuera del rango permitido.",
                    "Aceptar");
            }
        }

        // ==========================================
        // 7. CONVERTIR EL SIMBOLO DEL BOTON
        // ==========================================
        private static char NormalizeOperator(string symbol)
        {
            return symbol switch
            {
                "+" => '+',
                "−" => '-',
                "-" => '-',
                "×" => '*',
                "*" => '*',
                "÷" => '/',
                "/" => '/',
                _ => '\0'
            };
        }

        // ==========================================
        // 8. ACTUALIZAR LO QUE SE VE EN PANTALLA
        // ==========================================
        private void UpdateDisplay()
        {
            if (string.IsNullOrEmpty(_expression))
            {
                lblResultado.Text = "0";
                return;
            }

            // Mostrar los simbolos habituales de una calculadora.
            lblResultado.Text = _expression
                .Replace("*", "×")
                .Replace("/", "÷")
                .Replace("-", "−");
        }

        // ==========================================
        // 9. DETECTAR OPERADORES
        // ==========================================
        private static bool IsOperator(char character)
        {
            return character == '+' ||
                   character == '-' ||
                   character == '*' ||
                   character == '/';
        }

        private static bool EndsWithOperator(string expression)
        {
            return expression.Length > 0 &&
                   IsOperator(expression[expression.Length - 1]);
        }

        private static int FindLastOperatorIndex(string expression)
        {
            for (int i = expression.Length - 1; i >= 0; i--)
            {
                if (IsOperator(expression[i]))
                    return i;
            }

            return -1;
        }

        // ==========================================
        // 10. FORMATEAR EL RESULTADO
        // ==========================================
        private static string FormatNumber(double value)
        {
            if (value == 0)
                return "0";

            return value.ToString(
                "0.############################",
                CultureInfo.InvariantCulture);
        }

        // ==========================================
        // 11. MOSTRAR UN ERROR Y REINICIAR
        // ==========================================
        private void ShowError()
        {
            _expression = string.Empty;
            _currentEntry = "0";
            _startNewEntry = true;
            _hasResult = false;

            lblResultado.Text = "Error";
        }

        // ==========================================
        // 12. EVALUAR LA EXPRESION MATEMATICA
        // ==========================================
        private static double EvaluateExpression(string expression)
        {
            return new ExpressionParser(expression).Parse();
        }

        // Analizador matematico que respeta la prioridad
        // de multiplicacion y division sobre suma y resta.
        private sealed class ExpressionParser
        {
            private readonly string _text;
            private int _position;

            public ExpressionParser(string text)
            {
                _text = text;
                _position = 0;
            }

            public double Parse()
            {
                double result = ParseExpression();

                SkipSpaces();

                if (_position != _text.Length)
                    throw new FormatException();

                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException();

                return result;
            }

            // Suma y resta.
            private double ParseExpression()
            {
                double value = ParseTerm();

                while (true)
                {
                    SkipSpaces();

                    if (Match('+'))
                    {
                        value += ParseTerm();
                    }
                    else if (Match('-'))
                    {
                        value -= ParseTerm();
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            // Multiplicacion y division.
            private double ParseTerm()
            {
                double value = ParseFactor();

                while (true)
                {
                    SkipSpaces();

                    if (Match('*'))
                    {
                        value *= ParseFactor();
                    }
                    else if (Match('/'))
                    {
                        double divisor = ParseFactor();

                        if (divisor == 0)
                            throw new DivideByZeroException();

                        value /= divisor;
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            // Permite signos positivos y negativos.
            private double ParseFactor()
            {
                SkipSpaces();

                if (Match('+'))
                    return ParseFactor();

                if (Match('-'))
                    return -ParseFactor();

                return ParseNumber();
            }

            // Leer un numero decimal.
            private double ParseNumber()
            {
                SkipSpaces();

                int start = _position;
                bool hasDigit = false;
                bool hasDecimalPoint = false;

                while (_position < _text.Length)
                {
                    char character = _text[_position];

                    if (char.IsDigit(character))
                    {
                        hasDigit = true;
                        _position++;
                    }
                    else if (character == '.' && !hasDecimalPoint)
                    {
                        hasDecimalPoint = true;
                        _position++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (!hasDigit)
                    throw new FormatException();

                string numberText =
                    _text.Substring(start, _position - start);

                if (!double.TryParse(
                        numberText,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out double number))
                {
                    throw new FormatException();
                }

                return number;
            }

            private bool Match(char expected)
            {
                if (_position < _text.Length &&
                    _text[_position] == expected)
                {
                    _position++;
                    return true;
                }

                return false;
            }

            private void SkipSpaces()
            {
                while (_position < _text.Length &&
                       char.IsWhiteSpace(_text[_position]))
                {
                    _position++;
                }
            }
    }   }
}
