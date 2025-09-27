using System.Linq;
using WindowsInput;

namespace kiwiprojekt.tourbox.consoleapp
{
    class Program
    {
        static Stack<TourBoxEvent> keys = new();
        static IKeyboardSimulator keyboard = new InputSimulator().Keyboard;
        static IMouseSimulator mouse = new InputSimulator().Mouse;

        static void Main()
        {
            using var handler = new TourBoxHandler("COM3", TourBoxEventHandler);
            Console.ReadLine();
        }

        private static void TourBoxEventHandler(TourBoxEvent tourBoxEvent)
        {
            Console.WriteLine(tourBoxEvent);
            var code = tourBoxEvent.Code;
            var action = code & ~(1 << 7);
            Console.WriteLine($"Code: {code}, Action: {action}");
            //KeyMapper(tourBoxEvent);
            Console.WriteLine();
            
            switch (tourBoxEvent.Action)
            {
                case ActionType.Click:
                    ClickHook(tourBoxEvent);
                    if (keys.Count <= 1)
                    {
                        keys.Push(tourBoxEvent);
                    }
                    break;
                case ActionType.ClickReleased:
                    switch (keys.Count)
                    {
                        case 1:
                            var ev = keys.Pop();
                            SingleKeyHandler(ev.Keys[0]);
                            break;
                        case 2:
                            var key2 = keys.Pop();
                            var key1 = keys.Peek();

                            if (tourBoxEvent.Keys[0] == key2.Keys[0])
                            {
                                DoubleKeyHandler(key1.Keys[0], key2.Keys[0]);
                            }
                            break;
                    }
                    ClickReleaseHook(tourBoxEvent);
                    break;
                case ActionType.Increased:
                case ActionType.Decreased:
                    ScrollHandler(tourBoxEvent);
                    break;
            }

        }

        private static void ScrollHandler(TourBoxEvent tourBoxEvent)
        {
            if (keys.Count == 0)
            {
                if (tourBoxEvent.Is(ActionType.Increased, TourBoxKey.Scroll))
                {
                    mouse.VerticalScroll(2);
                }
                if (tourBoxEvent.Is(ActionType.Decreased, TourBoxKey.Scroll))
                {
                    mouse.VerticalScroll(-2);
                }
                if (tourBoxEvent.Is(ActionType.Increased, TourBoxKey.Knob))
                {
                    mouse.HorizontalScroll(2);
                }
                if (tourBoxEvent.Is(ActionType.Decreased, TourBoxKey.Knob))
                {
                    mouse.HorizontalScroll(-2);
                }
            }
            else if (keys.Peek().Keys[0] == TourBoxKey.Top)
            {
                if (tourBoxEvent.Is(ActionType.Increased, TourBoxKey.Scroll))
                {
                    mouse.MoveMouseBy(0, -10);
                }
                if (tourBoxEvent.Is(ActionType.Decreased, TourBoxKey.Scroll))
                {
                    mouse.MoveMouseBy(0, 10);
                }
                if (tourBoxEvent.Is(ActionType.Increased, TourBoxKey.Knob))
                {
                    mouse.MoveMouseBy(10, 0);
                }
                if (tourBoxEvent.Is(ActionType.Decreased, TourBoxKey.Knob))
                {
                    mouse.MoveMouseBy(-10, 0);
                }
            }
        }

        private static void ClickHook(TourBoxEvent tourBoxEvent)
        {
            switch (tourBoxEvent.Keys[0])
            {
                case TourBoxKey.Tall:
                    keyboard.KeyDown(VirtualKeyCode.CONTROL);
                    break;
                case TourBoxKey.Short:
                    keyboard.KeyDown(VirtualKeyCode.MENU);
                    break;
            }
        }

        private static void ClickReleaseHook(TourBoxEvent tourBoxEvent)
        {
            switch (tourBoxEvent.Keys[0])
            {
                case TourBoxKey.Tall:
                    keyboard.KeyUp(VirtualKeyCode.CONTROL);
                    break;
                case TourBoxKey.Short:
                    keyboard.KeyUp(VirtualKeyCode.MENU);
                    break;
            }
        }

        private static void SingleKeyHandler(TourBoxKey key)
        {
            switch (key)
            {
                case TourBoxKey.C1:
                    keyboard.KeyPress(VirtualKeyCode.BROWSER_BACK);
                    break;
                case TourBoxKey.C2:
                    keyboard.KeyPress(VirtualKeyCode.BROWSER_FORWARD);
                    break;
                case TourBoxKey.Tour:
                    keyboard.KeyDown(VirtualKeyCode.MENU)
                        .Sleep(100)
                        .KeyPress(VirtualKeyCode.SPACE)
                        .KeyUp(VirtualKeyCode.MENU);
                    break;
            }
        }

        private static void DoubleKeyHandler(TourBoxKey key1, TourBoxKey key2)
        {
            if (key1 == TourBoxKey.Top)
            {
                switch (key2)
                {
                    case TourBoxKey.Tall:
                        mouse.LeftButtonClick();
                        break;
                    case TourBoxKey.Short:
                        mouse.RightButtonClick();
                        break;
                    case TourBoxKey.Knob:
                    case TourBoxKey.Scroll:
                        mouse.MiddleButtonClick();
                        break;
                }
            }
        }
    }
}