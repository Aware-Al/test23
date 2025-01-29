using System;
using System.Collections.Generic;
using System.Text;
using AjaxChessBotHelperLib;

namespace AjaxChessBot
{
    public static class OnlineChessGameStateDecoder
    {
        public enum MoveNotation
        {
            uci,
            fen
        }

        public static string GetJavaScriptFromLichessHtmlCode(List<string> htmlCodes)
        {
            for (int i = htmlCodes.Count - 1; i >= 0; i--)
            {
                // Ищем скрипт с данными игры
                if (htmlCodes[i].Contains("id=\"page-init-data\"") || 
                    htmlCodes[i].Contains("lichess.load.then"))
                {
                    return htmlCodes[i];
                }
            }
            throw new ArgumentException("No JavaScript found in the html codes");
        }

        public static List<string> DecodeLichessMove(List<string> htmlCodes, MoveNotation moveNotation)
        {
            string jsScript = GetJavaScriptFromLichessHtmlCode(htmlCodes);
            return DecodeLichessMoveFromJavaScript(jsScript, moveNotation);
        }

        public static List<string> DecodeLichessMoveFromJavaScript(string jsScript, MoveNotation moveNotation)
        {
            List<string> moves = new List<string>();

            try
            {
                // Ищем начало JSON данных
                int jsonStartIndex = jsScript.IndexOf("{\"data\":");
                if (jsonStartIndex == -1)
                {
                    // Альтернативный поиск JSON данных
                    jsonStartIndex = jsScript.IndexOf("id=\"page-init-data\">");
                    if (jsonStartIndex != -1)
                    {
                        jsonStartIndex = jsScript.IndexOf("{", jsonStartIndex);
                    }
                }

                if (jsonStartIndex != -1)
                {
                    // Извлекаем JSON часть
                    int jsonEndIndex = jsScript.IndexOf("</script>", jsonStartIndex);
                    if (jsonEndIndex == -1)
                    {
                        jsonEndIndex = jsScript.Length;
                    }

                    string jsonData = jsScript.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex);

                    // В зависимости от типа нотации ищем нужные данные
                    switch (moveNotation)
                    {
                        case MoveNotation.fen:
                            ExtractFenPosition(jsonData, moves);
                            break;

                        case MoveNotation.uci:
                            ExtractLastMove(jsonData, moves);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing moves: {ex.Message}");
            }

            return moves;
        }

        private static void ExtractFenPosition(string jsonData, List<string> moves)
        {
            string fenKey = "\"fen\":\"";
            int currentIndex = 0;

            while (true)
            {
                int fenStart = jsonData.IndexOf(fenKey, currentIndex);
                if (fenStart == -1) break;

                fenStart += fenKey.Length;
                int fenEnd = jsonData.IndexOf("\"", fenStart);
                
                if (fenEnd != -1)
                {
                    string fenPosition = jsonData.Substring(fenStart, fenEnd - fenStart);
                    moves.Add(fenPosition);
                }

                currentIndex = fenEnd + 1;
            }
        }

        private static void ExtractLastMove(string jsonData, List<string> moves)
        {
            string moveKey = "\"lastMove\":\"";
            int currentIndex = 0;

            while (true)
            {
                int moveStart = jsonData.IndexOf(moveKey, currentIndex);
                if (moveStart == -1) break;

                moveStart += moveKey.Length;
                int moveEnd = jsonData.IndexOf("\"", moveStart);
                
                if (moveEnd != -1)
                {
                    string lastMove = jsonData.Substring(moveStart, moveEnd - moveStart);
                    moves.Add(lastMove);
                }

                currentIndex = moveEnd + 1;
            }
        }

        public static ChessProperty.PieceColor DecodeLichessPlayerColor(List<string> htmlCodes)
        {
            string jsScript = GetJavaScriptFromLichessHtmlCode(htmlCodes);
            return DecodeLichessPlayerColorFromJavaScript(jsScript);
        }

        public static ChessProperty.PieceColor DecodeLichessPlayerColorFromJavaScript(string jsScript)
        {
            ChessProperty.PieceColor playerColor = ChessProperty.PieceColor.white;

            try
            {
                // Ищем JSON данные
                int jsonStartIndex = jsScript.IndexOf("{\"data\":");
                if (jsonStartIndex != -1)
                {
                    int jsonEndIndex = jsScript.IndexOf("</script>", jsonStartIndex);
                    string jsonData = jsScript.Substring(jsonStartIndex, 
                        (jsonEndIndex != -1 ? jsonEndIndex : jsScript.Length) - jsonStartIndex);

                    // Ищем цвет игрока
                    string colorKey = "\"player\":\"";
                    int colorStart = jsonData.IndexOf(colorKey);
                    if (colorStart != -1)
                    {
                        colorStart += colorKey.Length;
                        int colorEnd = jsonData.IndexOf("\"", colorStart);
                        if (colorEnd != -1)
                        {
                            string playerColorString = jsonData.Substring(colorStart, colorEnd - colorStart);

                            if (playerColorString.ToLower() == "black")
                            {
                                playerColor = ChessProperty.PieceColor.black;
                            }
                            else if (playerColorString.ToLower() == "white")
                            {
                                playerColor = ChessProperty.PieceColor.white;
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid player color: {playerColorString}. Only black and white are available.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing player color: {ex.Message}");
            }

            return playerColor;
        }
    }
}
