[1. Парсер языка и построение АСТ](#Parser&AST)

[2. Pretty printer](#PrettyPrinter)

[3. AST-оптимизации свертки и устранения унарных операций](#OptExprFoldUnary&TransformUnaryToValue)

[4. AST-оптимизация замены сравнения с собой на true](#OptExprEqualToItself)

[5. AST-оптимизация замены сравнения переменной с собой на булевскую константу false](#OptExprSimilarNotEqual)

[6. AST-оптимизация умножения на единицу слева и справа, деления на единицу справа](#OptExprMultDivByOne)

[7. AST-оптимизация замены if(true) на его true ветку](#OptStatIfTrue)

[8. AST-оптимизация замены if(false) на его else ветку](#OptStatIfFalse)

[9. Генерация трехадресного кода](#GenerationTAC)

[10. Def-Use информация и удаление мертвого кода на ее основе](#DefUse)

[11. Живые и мёртвые переменные и удаление мёртвого кода (замена на пустой оператор)](#DeleteDeadCodeWithDeadVars)

[12. Устранение переходов к переходам](#GotoToGoto)

[13. Разбиение на ББл (от лидера до лидера)](#BasicBlockLeader)

[14. Интеграция оптимизаций трёхадресного кода между собой](#ThreeAddressCodeOptimizer)

[15. Анализ активных переменных](#LiveVariableAnalysis)

[16. Анализ достигающих определений](#ReachingDefinitions)

[17. Итерационный алгоритм в обобщённой структуре](#GenericIterativeAlgorithm)

[18. Построение дерева доминаторов](#DominatorTree)

[19. Определение всех естественных циклов](#naturalLoop)



<a name="Parser&AST"/>

### Парсер языка и построение АСТ

#### Постановка задачи
Написать лексер и парсер языка, используя GPLex и Yacc.
Реализовать построение абстрактного синтаксического дерева (АСТ).

#### Команда
А. Татарова, Т. Шкуро, А. Пацеев

#### Зависимые и предшествующие задачи
Зависимые:
- Базовые визиторы
- Генерация трехадресного кода
- Оптимизации по АСТ

#### Теоретическая часть
Решением этой задачи является реализация двух составляющих: лексера и парсера.

__Лексический анализатор__ (или лексер, токенизатор) — это часть программы, выполняющая разбор входной последовательности символов на токены. 

__Синтаксический анализатор__ (или парсер) — это часть программы, преобразующая входной текст (поток токенов) в структурированный формат, в данном случае происходит преобразование в АСТ.

__Абстрактное синтаксическое дерево__ (АСТ) — это ориентированное дерево, внутренние вершины сопоставлены с операторами языка, а листья — с соответствующими операндами. 

#### Практическая часть
Для реализации лексера и парсера был выбран Yacc+Lex, создающий front-end компилятора по описанию грамматики. Для этого генератора создаются два файла: SimpleLex.lex (описание лексера) и SimpleYacc.y (описание парсера). Далее генератор на основании этих файлов строит лексер и парсер на языке C#. 

##### Парсер языка
Парсер был реализован для языка со следующим синтаксисом:
```csharp
var a,b,c,d; //определение переменных
```
```csharp
//присваивание
a = 10; 
d = true;
```
```csharp
//операторы сравнения
d = a > 5;
d = b < a;
d = b != c;
d = a == b;
```
```csharp
//бинарные операции
a = a + 1;
b = a - 5;
c = b * a;
b = c / 2;
d = d or false;
d = d and true;
```
```csharp
//унарные операции
a = -b;
d = !(a < b);
```
```csharp
//полная форма условного оператора
if c > a
	a = c;
else {
    b = 1;
	a = b;
}
//Сокращенная форма условного оператора
if a < 1 
	c = 5 + 6 + 7; 
```

```csharp
//цикл while
while x < 25 { 
	x = x + 1; 
	x = x * 2; 
}
//цикл for - всегда идет на увеличение с шагом == 1
for i=2,7 
	a = a + i;
```

```csharp
input(a); //оператор ввода
print(a,b,c,d); //оператор вывода
```

```csharp
goto 777; //оператор безусловного перехода
//переход по метке
777: while (b < 20) 
    b = a + 5; 
```

Пример файла описания лексического анализатора (SimpleLex.lex)
```csharp
{INTNUM} { 
  yylval.iVal = int.Parse(yytext); 
  return (int)Tokens.INUM; 
}

"=" { return (int)Tokens.ASSIGN; }
";"  { return (int)Tokens.SEMICOLON; }
"+" {return (int)Tokens.PLUS; }
```

Пример файла описания синтаксического анализатора (SimpleYacc.y)
```csharp
%token <iVal> INUM
%token <bVal> BOOL
%token <sVal> ID

%type <eVal> expr ident A B C E T F exprlist
%type <stVal> assign statement for while if input print varlist var labelstatement goto block

stlist	: statement { $$ = new StListNode($1); }
		| stlist statement 
			{ 
				$1.Add($2); 
				$$ = $1; 
			}
		;
statement: assign SEMICOLON { $$ = $1; }
		| for { $$ = $1; }
		| while { $$ = $1; }
		| if { $$ = $1; }
		| block { $$ = $1; }
		| input SEMICOLON { $$ = $1; }
		| print SEMICOLON { $$ = $1; }
		| var SEMICOLON { $$ = $1; }
		| goto SEMICOLON { $$ = $1; }
		| labelstatement { $$ = $1; }
		;
```
Здесь в фигурных скобках указываются семантические действия (действия, происходящие для каждого распознанного правила грамматики и придающие смысл переводу программы в промежуточное представление).

##### Абстрактное синтаксическое дерево
В АСТ включаются узлы, соответствующие всем конструкциям языка. В узел записываются его существенные атрибуты. Например, для узла унарной операции `UnOpNode` такими атрибутами являются `Expr` и `Op` - соответственно выражение и операция, применяющаяся к этому выражению.  

```csharp
public class UnOpNode : ExprNode{
        public ExprNode Expr
        {
            get { return ExprChildren[0]; }
            set { ExprChildren[0] = value; }
        }
        public OpType Op { get; set; }
        public UnOpNode(ExprNode expr, OpType op)
        {
            Op = op;
            ExprChildren.Add(expr);
        }
    }
```

#### Место в общем проекте (Интеграция)
Создание грамматики и парсера и построение АСТ являются первыми шагами в работе компилятора. 

#### Пример работы
1. 
```charp
a = 5;
```
![Присваивание](https://github.com/Taally/FIIT_6_compiler/blob/master/Documentation/0_Parser%26AST/pic1.png)

2.
```charp
if a > 10
    b = a / 2;
else {
    b = a + b;
    a = b;
}
```
![Условный оператор](https://github.com/Taally/FIIT_6_compiler/blob/master/Documentation/0_Parser%26AST/pic2.png)

<a name="PrettyPrinter"/>

### Pretty printer

#### Постановка задачи
Создать визитор, которой по AST-дереву восстанавливает исходный код программы в отформатированном виде.

#### Команда
А. Татарова, Т. Шкуро

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* AST-дерево

#### Теоретическая часть
Для восстановления кода программы по AST необходимо совершить обход по дереву, сохраняя код в поле визитора Text. Класс визитора PrettyPrintVistor является наследником Visitor. Отступы создаются с помощью переменной Indent, увеличиваемую на 2 при входе в блок и уменьшаемую на 2 перед выходом из него.

#### Практическая часть
Список методов визитора для обхода узлов:
```csharp
void VisitBinOpNode(BinOpNode binop) 
void VisitUnOpNode(UnOpNode unop)
void VisitBoolValNode(BoolValNode b)
void VisitAssignNode(AssignNode a)
void VisitBlockNode(BlockNode bl)
void VisitStListNode(StListNode bl)
void VisitVarListNode(VarListNode v)
void VisitForNode(ForNode f)
void VisitWhileNode(WhileNode w)
void VisitLabelstatementNode(LabelStatementNode l)
void VisitGotoNode(GotoNode g)
void VisitIfElseNode(IfElseNode i)
void VisitExprListNode(ExprListNode e)
void VisitPrintNode(PrintNode p)
void VisitInputNode(InputNode i)
```
Примеры реализации методов визитора:
```csharp
public override void VisitAssignNode(AssignNode a)
{
    Text += IndentStr();
    a.Id.Visit(this);
    Text += " = ";
    a.Expr.Visit(this);
    Text += ";";
}

public override void VisitBlockNode(BlockNode bl)
{
    Text += "{" + Environment.NewLine;
    IndentPlus();
    bl.List.Visit(this);
    IndentMinus();
    Text += Environment.NewLine + IndentStr() + "}";
}
```

#### Место в общем проекте (Интеграция)
Визитор используется после создания парсером AST-дерева:
```csharp
Scanner scanner = new Scanner();
scanner.SetSource(Text, 0);
Parser parser = new Parser(scanner);
var pp = new PrettyPrintVisitor();
parser.root.Visit(pp);
Console.WriteLine(pp.Text);
```

#### Пример работы
Исходный код программы:
```csharp
var a, b, c, d, i; 
a = 5 + 3 - 1;
b = (a - 3) / -b;
if a > b 
{
c = 1;
} else c = 2;
for i=1,5
c = c+1;
d = a <= b;
if c == 6 goto 777;
d = d or a < 10;
777: while c < 25 { 
a = a + 3; 
b = b * 2; 
}
```
Результат работы PrettyPrintVisitor:
```csharp
var a, b, c, d, i;
a = ((5 + 3) - 1);
b = ((a - 3) / (-b));
if (a > b) {
  c = 1;
}
else
  c = 2;
for i = 1, 5
  c = (c + 1);
d = (a <= b);
if (c == 6)
  goto 777;
d = (d or (a < 10));
777: while (c < 25) {
  a = (a + 3);
  b = (b * 2);
}
```

<a name="OptExprFoldUnary&TransformUnaryToValue"/>

### AST-оптимизации свертки и устранения унарных операций
#### Постановка задачи
Реализовать оптимизации по AST дереву:
1. Свертка двух унарных операций
- op a == op a => True
- op a != op a => False
где op = {!, -}
- !a == a => False
- a != !a => True
2. Устранение унарной операции
- Превращение унарной операции «минус» с узлом целых чисел с Num\==1 в узел целых чисел с Num==-1
- !True=> False и !False => True
- !!a=>a
- \-\-a => a

#### Команда
А. Татарова, Т. Шкуро
#### Зависимые и предшествующие задачи
Предшествующие:
- Построение AST-дерева
- Базовые визиторы

#### Теоретическая часть
Данные оптимизации должны по необходимым условиям преобразовать поддерево АСТ таким образом:

1. 

![Оптимизация свертки унарных операций](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/1_OptExprFoldUnary%26TransformUnaryToValue/pic1.png)

1. 

![Оптимизация устранения унарных операций](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/1_OptExprFoldUnary%26TransformUnaryToValue/pic2.png)


#### Практическая часть
1. Свертка двух унарных операций
Данная оптимизация заходит только в узлы бинарных операций. Прежде всего проверяются необходимые условия: левый и правый операнды представляют собой узлы унарных операций и тип бинарной операции "равно" или "неравно". После разбирается, что в этих операндах только одна и так же переменная/константа, что тип унарных операций одинаков и т.д. Если условия выполняются, в родительском узле происходит замена бинарной операции на значение Boolean. В противном случае узел обрабатывается по умолчанию.
```csharp
public override void VisitBinOpNode(BinOpNode binop)
{
    var left = binop.Left as UnOpNode;
    var right = binop.Right as UnOpNode;
    
    if (left != null && right != null && left.Op == right.Op && left.Expr is IdNode idl)
    {
        if (right.Expr is IdNode idr && idl.Name == idr.Name)
        {
            if (binop.Op == OpType.EQUAL)
            {
                ReplaceExpr(binop, new BoolValNode(true));
            }
            if (binop.Op == OpType.NOTEQUAL)
            {
                ReplaceExpr(binop, new BoolValNode(false));
            }
        }
    }
    else
    if (left != null && left.Op == OpType.NOT && left.Expr is IdNode
        && binop.Right is IdNode && (left.Expr as IdNode).Name == (binop.Right as IdNode).Name)
    {
        /*...*/
    }
    else
    if (right != null && right.Op == OpType.NOT && right.Expr is IdNode
        && binop.Left is IdNode && (right.Expr as IdNode).Name == (binop.Left as IdNode).Name)
    {
        /*...*/
    }
    else
    {
        base.VisitBinOpNode(binop);
    }
}
```

2. Устранение унарных операций
Данная оптимизация работает с узлами унарных операций. Прежде всего проверяется: выражение должно быть переменной или константой. Если условие не выполняется, то узел обрабатывается по умолчанию.
Если условие выполняется, то производятся следующие проверки и действия при их выполнении:
- если выражение было целочисленной константой, в родительском узле происходит замена унарной операции на узел целых чисел со значением, умноженным на -1;
- если выражение было значением Boolean, в родительском узле происходит замена унарной операции на  значение Boolean, взятое с отрицанием (было !true, стало false);
- если выражение было переменной, то дополнительно проверяется, является ли родительский узел так же унарной операцией с тем же типом операции. Если является, то в родительском узле второго порядка происходит замена выражения на переменную. 

```csharp
public override void VisitUnOpNode(UnOpNode unop)
{
    if (unop.Expr is IntNumNode num)
    {
        if (unop.Op == OpType.UNMINUS)
        {
            ReplaceExpr(unop, new IntNumNode(-1 * num.Num));
        }
        //...
    }
    else if (unop.Expr is BoolValNode b)
    {
        if (unop.Op == OpType.NOT)
        {
            ReplaceExpr(unop, new BoolValNode(!b.Val));
        }
        //...
    }
    else if (unop.Expr is IdNode)
    {
        if (unop.Parent is UnOpNode && (unop.Parent as UnOpNode).Op == unop.Op)
        {
            ReplaceExpr(unop.Parent as UnOpNode, unop.Expr);
        }
    }
    else
    {
        base.VisitUnOpNode(unop);
    }
}
```

#### Место в общем проекте (Интеграция)
Данные оптимизации выполняются вместе с остальными АСТ оптимизациями после построения абстрактного синтаксического дерева, но до генерации трехадресного кода.

#### Пример работы
1. Свертка двух унарных операций
- До 
```csharp
c = ((-a) != (-a));
a = (b != (!b));
d = ((!b) == (!b));
b = ((!c) == c);
```
- После
```csharp
c = false;
a = true;
d = true;
b = false;
```
2. Устранение унарных операций
- До
```csharp
a = (!true);
a = (a - (-(-1)));
d = (!(!(!b)));
a = (a - (-(-(-b))));
```
- После
```csharp
a = false;
a = (a - 1);
d = (!b);
a = (a - (-b)); // здесь первый минус - бинарный
```
<a name="OptExprEqualToItself"/>

### AST-оптимизация замены сравнения с собой на true

#### Постановка задачи
Реализовать оптимизацию по AST-дереву — замена сравнения с собой на true:
- a == a => true
- a <= a => true
- a >= a => true

#### Команда
Д. Володин, Н. Моздоров

#### Зависимые и предшествующие задачи
Предшествующие:
- Построение AST-дерева
- Базовые визиторы

#### Теоретическая часть
Данная оптимизация выполняется на AST-дереве, построенном для данной программы. Необходимо найти в нём узлы, содержащие операции сравнения (==, <=, >=) с одной и той же переменной, и заменить эти сравнения на True.

#### Практическая часть
Нужная оптимизация производится с применением паттерна Visitor, для этого созданный класс наследует `ChangeVisitor` и
переопределяет метод `PostVisit`.
```csharp
internal class OptExprEqualToItself : ChangeVisitor
{
    public override void PostVisit(Node n)
    {
        if (n is BinOpNode binop)
        {
            if (binop.Left is IdNode Left && binop.Right is IdNode Right && Left.Name == Right.Name &&
            (binop.Op == OpType.EQUAL || binop.Op == OpType.EQLESS || binop.Op == OpType.EQGREATER))
            {
                ReplaceExpr(binop, new BoolValNode(true));
            }
        }
    }
}
```

#### Место в общем проекте (Интеграция)
Данная оптимизация применяется в классе `ASTOptimizer` наряду со всеми остальными оптимизациями по AST-дереву.

#### Пример работы
До:
```
var a, b;
a = 1;
b = (a == a);
b = (a <= a);
b = (a >= a);
```
После:
```
var a, b;
a = 1;
b = true;
b = true;
b = true;
```

<a name="OptExprSimilarNotEqual"/>

### AST-оптимизация замены сравнения переменной с собой на булевскую константу false

#### Постановка задачи
Реализовать оптимизацию по AST дереву вида (a > a, a != a ) = False

#### Команда
К. Галицкий, А. Черкашин

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* AST дерево

#### Теоретическая часть
Реализовать оптимизацию по AST дереву вида (a > a, a != a ) = False:
  * До
  ```csharp
  #t1 = a > a;
  ```
  * После
  ```csharp
  #t1 = False;
  ```
  * До
  ```csharp
  #t1 = a != a;
  ```
  * После
  ```csharp
  #t1 = False;
  ```

#### Практическая часть
Примеры реализации метода:

```csharp
        if (
                // Для цифр и значений bool :
                (binop.Left is IntNumNode && binop.Right is IntNumNode && (binop.Left as IntNumNode).Num == (binop.Right as IntNumNode).Num && (binop.Op == OpType.GREATER || binop.Op == OpType.LESS))
                || ((binop.Left is BoolValNode && binop.Right is BoolValNode && (binop.Left as BoolValNode).Val == (binop.Right as BoolValNode).Val && (binop.Op == OpType.GREATER || binop.Op == OpType.LESS)))
                || ((binop.Left is IntNumNode && binop.Right is IntNumNode && (binop.Left as IntNumNode).Num == (binop.Right as IntNumNode).Num && binop.Op == OpType.NOTEQUAL))
                || ((binop.Left is BoolValNode && binop.Right is BoolValNode && (binop.Left as BoolValNode).Val == (binop.Right as BoolValNode).Val && binop.Op == OpType.NOTEQUAL))
                // Для переменных :
                || ((binop.Left is IdNode && binop.Right is IdNode && (binop.Left as IdNode).Name == (binop.Right as IdNode).Name && (binop.Op == OpType.GREATER || binop.Op == OpType.LESS)))
                || ((binop.Left is IdNode && binop.Right is IdNode && (binop.Left as IdNode).Name == (binop.Right as IdNode).Name && binop.Op == OpType.NOTEQUAL))
                )
            {
                binop.Left.Visit(this);
                binop.Right.Visit(this); // сделать то же в правом поддереве
                ReplaceExpr(binop, new BoolValNode(false)); // Заменить себя на своё правое поддерево
            }
            else
            {
                base.VisitBinOpNode(binop);
            }
```

#### Место в общем проекте (Интеграция)
```csharp
public static List<ChangeVisitor> Optimizations { get; } = new List<ChangeVisitor>
       {
             /* ... */
           new OptExprSimilarNotEqual(),
             /* ... */
       };

       public static void Optimize(Parser parser)
       {
           int optInd = 0;
           do
           {
               parser.root.Visit(Optimizations[optInd]);
               if (Optimizations[optInd].Changed)
                   optInd = 0;
               else
                   ++optInd;
           } while (optInd < Optimizations.Count);
       }
```

#### Пример работы
Исходный код программы:
```csharp
var a, b;
b = 5
a = b > b
```
Результат работы:
```csharp
var a, b;
b = 5;
a = false;
```

<a name="OptExprMultDivByOne"/>

### AST-оптимизация умножения на единицу слева и справа, деления на единицу справа

#### Постановка задачи
Реализовать оптимизацию по AST дереву вида a\*1 = a, 1\*a = a, a/1 = a

#### Команда
А. Татарова, Т. Шкуро

#### Зависимые и предшествующие задачи
Предшествующие:
- Построение AST-дерева
- Базовые визиторы
- ChangeVisitor (?)

#### Теоретическая часть
Эта оптимизация представляет собой визитор, унаследованный от ChangeVisitor и меняющий ссылки между узлами ACT.
Рассмотрим некие узлы АСТ:

![Узлы AСT до оптимизации](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/1_OptExprMultDivByOne/pic1.png)

Эта блок-схема соответствует строчке  ```b = a * 1```.
Данная оптимизация должна отработать так: ``` b = a ```.
Блок-схема ниже показывает, что происходит с деревом после применения этой оптимизации:

![Узлы AСT после оптимизации](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/1_OptExprMultDivByOne/pic1.png)

#### Практическая часть
Алгоритм заходит только в узлы бинарных операций. Прежде всего проверяются необходимые условия: тип операции либо умножение, либо деление и что один из операндов это единица. Если условия выполняются, в родительском узле происходит замена бинарной операции на переменную. В противном случае узел обрабатывается по умолчанию.
```csharp
internal class OptExprMultDivByOne : ChangeVisitor
{
    public override void VisitBinOpNode(BinOpNode binop)
    {
		switch (binop.Op)
        {
            case OpType.MULT:
                if (binop.Left is IntNumNode && (binop.Left as IntNumNode).Num == 1)
                {
                    binop.Right.Visit(this);
                    ReplaceExpr(binop, binop.Right);
                }
                else if (binop.Right is IntNumNode && (binop.Right as IntNumNode).Num == 1)
                {
                    binop.Left.Visit(this);
                    ReplaceExpr(binop, binop.Left);
                }
                else
                {
				    base.VisitBinOpNode(binop);
                }
                break;

            case OpType.DIV:
                if (binop.Right is IntNumNode && (binop.Right as IntNumNode).Num == 1)
                {
                    binop.Left.Visit(this);
                    ReplaceExpr(binop, binop.Left);
                }
                break;

            default:
                base.VisitBinOpNode(binop);
                break;
            }
        }
    }
```

#### Место в общем проекте (Интеграция)
Данная оптимизация выполняется вместе с остальными АСТ оптимизациями после построения абстрактного синтаксического дерева, но до генерации трехадресного кода. 

#### Пример работы
- До 
```csharp
var a, b, c;
a = (b * 1);
b = (1 * a);
c = (a / 1);
a = (1 * 5);
b = (1 / 1);
```
- После
```csharp
var a, b, c;
a = b;
b = a;
c = a;
a = 5;
b = 1;
```
- До
```csharp
var a, b, c; 
a = a * (b * 1) / 1;
b = b + (1 * (c + a));
```
- После
```csharp
var a, b, c;
a = (a * b);
b = (b + (c + a));
```
<a name="OptStatIfTrue"/>

### AST-оптимизация замены if(true) на его true ветку

#### Постановка задачи
Реализовать оптимизацию по AST дереву вида if(true) st1 else st2 => st1

#### Команда
А. Татарова, Т. Шкуро, Д. Володин, Н. Моздоров

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* AST дерево

#### Теоретическая часть
Реализовать оптимизацию по AST дереву вида if(true) st1 else st2 => st1
  * До
  ```csharp
  if(true)
    st1;
  else
    st2;
  ```
  * После
  ```csharp
  st1;
  ```

#### Практическая часть
Примеры реализации метода:

```csharp
    if (n is IfElseNode ifNode)  // Если это корень if
        if (ifNode.Expr is BoolValNode boolNode && boolNode.Val) // Если выражение == true
        {
            if (ifNode.TrueStat != null)
            {
                ifNode.TrueStat.Visit(this);
            }
            ReplaceStat(ifNode, ifNode.TrueStat);
        }
```

#### Место в общем проекте (Интеграция)
```csharp
public static List<ChangeVisitor> Optimizations { get; } = new List<ChangeVisitor>
       {
             /* ... */
           new OptStatIftrue(),
             /* ... */
       };

       public static void Optimize(Parser parser)
       {
           int optInd = 0;
           do
           {
               parser.root.Visit(Optimizations[optInd]);
               if (Optimizations[optInd].Changed)
                   optInd = 0;
               else
                   ++optInd;
           } while (optInd < Optimizations.Count);
       }
```

#### Пример работы
Исходный код программы:
```csharp
var a;
a = 14;
if(true)
  a = a - 4;
else
  a = a + 10;
```
Результат работы:
```csharp
a = 14;
a = a - 4;
```

<a name="OptStatIfFalse"/>

### AST-оптимизация замены if(false) на его else ветку

#### Постановка задачи
Реализовать оптимизацию по AST дереву вида if(false) st1 else st2 => st2

#### Команда
К. Галицкий, А. Черкашин

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* AST дерево

#### Теоретическая часть
Реализовать оптимизацию по AST дереву вида if(false) st1 else st2 => st2
  * До
  ```csharp
  if(false)
    st1;
  else
    st2;
  ```
  * После
  ```csharp
  st2;
  ```

#### Практическая часть
Примеры реализации метода:

```csharp
    if (n is IfElseNode ifNode)  // Если это корень if
        if (ifNode.Expr is BoolValNode boolNode && boolNode.Val == false) // Если выражение == false
        {
            if (ifNode.FalseStat != null)  // Если ветка fasle не NULL
            {
                ifNode.FalseStat.Visit(this);
                ReplaceStat(ifNode, ifNode.FalseStat);  //  Меняем наш корень на ветку else
            }
            else 
            {
                ReplaceStat(ifNode, new EmptyNode());
            }
        }
```

#### Место в общем проекте (Интеграция)
```csharp
public static List<ChangeVisitor> Optimizations { get; } = new List<ChangeVisitor>
       {
             /* ... */
           new OptStatIfFalse(),
             /* ... */
       };

       public static void Optimize(Parser parser)
       {
           int optInd = 0;
           do
           {
               parser.root.Visit(Optimizations[optInd]);
               if (Optimizations[optInd].Changed)
                   optInd = 0;
               else
                   ++optInd;
           } while (optInd < Optimizations.Count);
       }
```

#### Пример работы
Исходный код программы:
```csharp
var a, b;
b = 5
if(false)
  a = 3;
else
  a = 57;
```
Результат работы:
```csharp
b = 5;
a = 57;
```

<a name="GenerationTAC"/>

### Генерация трехадресного кода

#### Постановка задачи

Реализовать генерацию трехадресного кода для всех инструкций языка

#### Команда

Д. Володин, А. Татарова, Т. Шкуро

#### Зависимые и предшествующие задачи

Предшествующие:
- Построение АСТ
- Базовые визиторы

Зависимые: 
- Разбиение на базовые блоки

#### Теоретическая часть

__Трехадресный код__ (ТАК) — это линеаризованное абстрактное синтаксическое дерево, из которого восстановить текст программы уже нельзя. В трехадресном коде в правой части выражении допускается только один оператор, т.е. выражение ```x+y*z``` транслируется как
```
t1 = y * z
t2 = x + t1
```
где ```t1```,```t2``` – временные переменные.

На примере ниже можно увидеть разбор АСТ узлов, соответствующих выражению ```a = a + b * c```

![Пример трехадресного кода](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/2_GenerationTAC/pic1.jpg)

Представление треахдресного кода является четверкой полей 
(op, arg1, arg2, res). На рисунке ниже показано, как разбирается выражение ```a = b * (-c) + b * (-c)``` в виде треахдресного кода и представляется в таблице четверками:

![Пример четверок трехадресного кода](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/2_GenerationTAC/pic2.jpg)

Для хранения меток перехода добавляется еще одно поле Label, и тогда транслируемые инструкции становятся пятерками полей. 

#### Практическая часть

Для транслирования АСТ в трехадресный код создан класс Instruction, в котором хранится пятерка полей 

```csharp
public string Label { get; internal set; }
public string Operation { get; }
public string Argument1 { get; }
public string Argument2 { get; }
public string Result { get; }
```
Генератор трехадресного кода представляет собой визитор, обходящий все узлы и генерирующий определенные инструкции в зависимости от типа узла:
- для выражений
```csharp
private string Gen(ExprNode ex)
{
    if (ex.GetType() == typeof(BinOpNode))
    {
        var bin = (BinOpNode)ex;
        var argument1 = Gen(bin.Left);
        var argument2 = Gen(bin.Right);
        var result = ThreeAddressCodeTmp.GenTmpName();
        GenCommand("", bin.Op.ToString(), argument1, argument2, result);
        return result;
    }
    else if (ex.GetType() == typeof(UnOpNode))
    {
        /*..*/
    }
    else if (ex.GetType() == typeof(IdNode))
    {
        var id = (IdNode)ex;
        return id.Name;
    }
    else if (ex.GetType() == typeof(IntNumNode))
    {
        /*..*/
    }
    else if (ex.GetType() == typeof(BoolValNode))
    {
        /*..*/
    }
    return null;
}
```
- для оператора присваивания
```csharp
public override void VisitAssignNode(AssignNode a)
{
    var argument1 = Gen(a.Expr);
    GenCommand("", "assign", argument1, "", a.Id.Name);
}
```
- для условного оператора
```csharp
public override void VisitIfElseNode(IfElseNode i)
{
    var exprTmpName = Gen(i.Expr);
    var trueLabel = ThreeAddressCodeTmp.GenTmpLabel();
    if (i.TrueStat is LabelStatementNode label)
    {
        trueLabel = label.Label.Num.ToString();
    }
    else
    if (i.TrueStat is BlockNode block
        && block.List.StatChildren[0] is LabelStatementNode labelB)
    {
        trueLabel = labelB.Label.Num.ToString();
    }
    var falseLabel = ThreeAddressCodeTmp.GenTmpLabel();
    GenCommand("", "ifgoto", exprTmpName, trueLabel, "");

    i.FalseStat?.Visit(this);
    GenCommand("", "goto", falseLabel, "", "");
    var instructionIndex = Instructions.Count;
    
    i.TrueStat.Visit(this);
    Instructions[instructionIndex].Label = trueLabel;
    GenCommand(falseLabel, "noop", "", "", "");
}
```
- для цикла while
```csharp
 public override void VisitWhileNode(WhileNode w)
{
    var exprTmpName = Gen(w.Expr);
    var whileHeadLabel = ThreeAddressCodeTmp.GenTmpLabel();
    var whileBodyLabel = ThreeAddressCodeTmp.GenTmpLabel();
    var exitLabel = ThreeAddressCodeTmp.GenTmpLabel();

    Instructions[Instructions.Count - 1].Label = whileHeadLabel;
    GenCommand("", "ifgoto", exprTmpName, whileBodyLabel, "");
    GenCommand("", "goto", exitLabel, "", "");
    var instructionIndex = Instructions.Count;
    w.Stat.Visit(this);
    Instructions[instructionIndex].Label = whileBodyLabel;
    GenCommand("", "goto", whileHeadLabel, "", "");
    GenCommand(exitLabel, "noop", "", "", "");
}
```
- для цикла for (необходимо отметить: здесь делается допущение, что for шагает на +1 до границы, не включая ее)
```csharp
 public override void VisitForNode(ForNode f)
{
    var Id = f.Id.Name;
    var forHeadLabel = ThreeAddressCodeTmp.GenTmpLabel();
    var exitLabel = ThreeAddressCodeTmp.GenTmpLabel();
    var fromTmpName = Gen(f.From);
    GenCommand("", "assign", fromTmpName, "", Id);

    var toTmpName = Gen(f.To);
    var condTmpName = ThreeAddressCodeTmp.GenTmpName();
    GenCommand(forHeadLabel, "EQGREATER", Id, toTmpName, condTmpName);
    GenCommand("", "ifgoto", condTmpName, exitLabel, "");
    f.Stat.Visit(this);
    GenCommand("", "PLUS", Id, "1", Id);
    GenCommand("", "goto", forHeadLabel, "", "");
    GenCommand(exitLabel, "noop", "", "", "");
}
```
- для input и print
```csharp
public override void VisitInputNode(InputNode i) => GenCommand("", "input", "", "", i.Ident.Name);
public override void VisitPrintNode(PrintNode p)
{
    foreach (var x in p.ExprList.ExprChildren)
    {
        var exprTmpName = Gen(x);
        GenCommand("", "print", exprTmpName, "", "");
    }
}
```

- для goto и узла метки перехода
```csharp
public override void VisitGotoNode(GotoNode g) => GenCommand("", "goto", g.Label.Num.ToString(), "", "");
public override void VisitLabelstatementNode(LabelStatementNode l)
{
    var instructionIndex = Instructions.Count;
    if (l.Stat is WhileNode)
    {
        GenCommand("", "noop", "", "", "");
    }
    l.Stat.Visit(this);
    Instructions[instructionIndex].Label = l.Label.Num.ToString();
}
```
- для пустого оператора
```csharp
public override void VisitEmptyNode(EmptyNode w) => GenCommand("", "noop", "", "", "");
```
где ```GenCommand``` --- функция, создающая инструкцию с заданной пятеркой полей.

#### Место в общем проекте (Интеграция)

Генерация трехадресного кода происходит после построения АСТ дерева и применения оптимизаций по нему, после генерации происходит разбиение трехадресного кода на блоки.
```csharp
ASTOptimizer.Optimize(parser);
/*..*/

var threeAddrCodeVisitor = new ThreeAddrGenVisitor();
parser.root.Visit(threeAddrCodeVisitor);
var threeAddressCode = threeAddrCodeVisitor.Instructions;
/*..*/
```

#### Пример работы

- АСТ дерево после оптимизаций
```
var a, b, c, d, x, zz, i;
goto 777;
777: while ((x < 25) or (a > 100)) {
  x = (x + 1);
  x = (x * 2);
}
for i = 2, 7
  x = (x + 1);
zz = (((a * (b + 1)) / c) - (b * a));
input(zz);
print(zz, a, b);
if (c > a) {
  a = c;
  a = 1;
}
else {
  b = 1;
  a = b;
}
```

- Сгенерированный трехадресный код
```
goto 777
777: noop
#t1 = x < 25
#t2 = a > 100
L1: #t3 = #t1 or #t2
if #t3 goto L2
goto L3
L2: #t4 = x + 1
x = #t4
#t5 = x * 2
x = #t5
goto L1
L3: noop
i = 2
L4: #t6 = i >= 7
if #t6 goto L5
#t7 = x + 1
x = #t7
i = i + 1
goto L4
L5: noop
#t8 = b + 1
#t9 = a * #t8
#t10 = #t9 / c
#t11 = b * a
#t12 = #t10 - #t11
zz = #t12
input zz
print zz
print a
print b
#t13 = c > a
if #t13 goto L6
b = 1
a = b
goto L7
L6: a = c
a = 1
L7: noop
```

<a name="DefUse"/>

### Def-Use информация и удаление мертвого кода на ее основе 

#### Постановка задачи
Накопление Def-Use информации в пределах ББл и удаление мертвого кода на основе этой информации.

#### Команда
А. Татарова, Т. Шкуро

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* Трехадресный код
* Разбиение кода на базовые блоки

#### Теоретическая часть
В рамках этой задачи переменные делятся на два типа: **def** и **use**.
Def --- это определение переменной, т.е. этой переменной было присвоено значение в данном ББл.
Use --- это использование переменной, т.е. эта переменная использовалась в каком-либо выражении в данном ББл.
Например, в следующем выражении **a** будет являться def-переменной, а **b** и **c** - use-переменными:
```
a = b + c;
```
На основе трехадресного кода составляется список Def-Use: список **def**-переменных, где для каждой **def**-переменной есть список использований этой переменной, т.е. список **use**.
После формирования Def-Use информации по всему коду ББл производится удаление мертвого кода --- удаление определений тех переменных, список использования которых пуст. Для удаления мертвого кода список команд проходится снизу вверх, при удалении команды производится обновление информации Def-Use. 

#### Практическая часть
Первым шагом по полученным командам трехадресного кода составляется список Def-Use:
```csharp
public class Use
{
    public Def Parent { get; set; } // определение переменной
    public int OrderNum { get; set; } // номер команды в трехадресном коде
}

public class Def
{
    public List<Use> Uses { get; set; } // список использований переменной
    public int OrderNum { get; set; } // номер команды в трехадресном коде
    public string Id { get; set; } // идентификатор переменной
}
    
public static List<Def> DefList;

private static void FillLists(List<Instruction> commands)
{
    DefList = new List<Def>();
    for (int i = 0; i < commands.Count; ++i)
    {
        // если оператор является оператором присваивания, опертором 
        // ариметических или логических операций или оператором ввода,
        // добавляем в список DefList результат этой операции
        if (operations.Contains(commands[i].Operation))
            DefList.Add(new Def(i, commands[i].Result));
        // если в правой части оператора переменные,
        // и их определение есть в списке DefList,
        // добавляем их в соотвествующий список Uses
        AddUse(commands[i].Argument1, commands[i], i);
        AddUse(commands[i].Argument2, commands[i], i);
    }
}
```
Далее производится анализ полученной информации, начиная с последней команды трехадресного кода. Определение переменной можно удалить, если
1. список ее использований пуст
2. если эта переменная не является временной (появившейся в результате создания трехадресного кода), то это не должно быть ее последним определением в ББл (т.к. эта переменная может использоваться в следующих блоках)

```csharp
for (int i = commands.Count - 1; i >= 0; --i)
{
    // для текущей команды находим ее индекс в списке DefList 
    var c = commands[i];
    var curDefInd = DefList.FindIndex(x => x.OrderNum == i);
    // а также находим индекс ее последнего определения в ББл
    var lastDefInd = DefList.FindLastIndex(x => x.Id == c.Result);
    
    // если для текущей переменной существует определение в ББл,
    // проверяем, можно ли удалить команду
    if (curDefInd != -1 && DefList[curDefInd].Uses.Count == 0
            && (c.Result[0] != '#' ? curDefInd != lastDefInd : true))
    {
        // при удалении команды переменные в ее правой части 
        // удаляются из соответствующих списков Uses
        DeleteUse(commands[i].Argument1, i);
        DeleteUse(commands[i].Argument2, i);
        // вместо удаленной команды добавляется пустой оператор noop
        result.Add(new Instruction(commands[i].Label, "noop", null, null, null));
    }
    // если удалять не нужно, добавляем команду в результирующий список команд
    else result.Add(commands[i]);
}
```

#### Место в общем проекте (Интеграция)
Удаление мертвого кода является одной из оптимизаций, применяемых к трехадресному коду:
```csharp
/* ThreeAddressCodeOptimizer.cs */
private static List<Optimization> BasicBlockOptimizations => new List<Optimization>()
{
    ThreeAddressCodeDefUse.DeleteDeadCode,
    /* ... */
};
private static List<Optimization> AllCodeOptimizations => new List<Optimization>
{ /* ... */ };

public static List<Instruction> OptimizeAll(List<Instruction> instructions) =>
    Optimize(instructions, BasicBlockOptimizations, AllCodeOptimizations);
    
/* Main.cs */
var threeAddrCodeVisitor = new ThreeAddrGenVisitor();
parser.root.Visit(threeAddrCodeVisitor);
var threeAddressCode = threeAddrCodeVisitor.Instructions;
var optResult = ThreeAddressCodeOptimizer.OptimizeAll(threeAddressCode);
```

#### Тесты
В тестах проверяется, что для заданного трехадресного кода ББл оптимизация возвращает ожидаемый результат:
```csharp
[Test]
public void VarAssignSimple()
{
    var TAC = GenTAC(@"
    var a, b, x;
    x = a;
    x = b;
    ");
    var optimizations = new List<Optimization> { 
        ThreeAddressCodeDefUse.DeleteDeadCode 
    };
    var expected = new List<string>() 
    {
        "noop",
        "x = b"
    };
    var actual = ThreeAddressCodeOptimizer.Optimize(TAC, optimizations)
        .Select(instruction => instruction.ToString());
    CollectionAssert.AreEqual(expected, actual);
}

[Test]
public void NoDeadCode()
{
    var TAC = GenTAC(@"
    var a, b, c;
    a = 2;
    b = a + 4;
    c = a * b;
    ");
    var optimizations = new List<Optimization> { 
        ThreeAddressCodeDefUse.DeleteDeadCode
    };
    var expected = new List<string>()
    {
        "a = 2",
        "#t1 = a + 4",
        "b = #t1",
        "#t2 = a * b",
        "c = #t2"
    };
    var actual = ThreeAddressCodeOptimizer.Optimize(TAC, optimizations)
        .Select(instruction => instruction.ToString());
    CollectionAssert.AreEqual(expected, actual);
}

[Test]
public void DeadInput()
{
    var TAC = GenTAC(@"
    var a, b;
    input(a);
    input(a);
    b = a + 1;
    ");
    var optimizations = new List<Optimization> { 
        ThreeAddressCodeDefUse.DeleteDeadCode
    };
    var expected = new List<string>()
    {
        "noop",
        "input a",
        "#t1 = a + 1",
        "b = #t1"
    };
    var actual = ThreeAddressCodeOptimizer.Optimize(TAC, optimizations)
        .Select(instruction => instruction.ToString());
    CollectionAssert.AreEqual(expected, actual);
}
```
<a name="DeleteDeadCodeWithDeadVars"/>


### Живые и мёртвые переменные и удаление мёртвого кода (замена на пустой оператор)
#### Постановка задачи
Необходимо в пределах одного базового блока определить живые и мёртвые переменные, а также заменить на пустой оператор присваивания мёртвым переменным.
#### Команда
Д. Володин, Н. Моздоров
#### Зависимые и предшествующие задачи
Предшествующие: 
- Генерация трёхадресного кода

Зависимые:
- Интеграция оптимизаций трёхадресного кода между собой

#### Теоретическая часть
Пусть в трёхадресном коде имеются два оператора присваивания, такие что в первом некоторая переменная `x` стоит в левой части, а во втором переменная `x` стоит в правой части, причём первое присваивание стоит перед вторым. Если среди команд, стоящих между этими двумя присваиваниями, переменная `x` не переопределяется, то говорят, что на этом участке кода переменная `x` живая, иначе --- мёртвая. 

Анализ того, является ли переменная живой, выполняется снизу вверх, начиная с последней инструкции в базовом блоке. В конце блока все переменные объявляются живыми, затем для каждой команды проверяется: если выполняется присваивание переменной `x`, то она объявляется мёртвой, а все переменные, стоящие в правой части, объявляются живыми. Если при проходе снизу вверх встречается команда `x = <выражение>` и переменная `x` на данный момент является мёртвой, то такое присваивание является мёртвым кодом, и его можно удалить.

#### Практическая часть
Оптимизация выполняется в классе `DeleteDeadCodeWithDeadVars`, в методе `DeleteDeadCode`. Вначале создаются новый список инструкций, который будет возвращён методом, и словарь, хранящий состояния переменных.
```csharp
var newInstructions = new List<Instruction>();
var varStatus = new Dictionary<string, bool>();
```

Затем отдельно обрабатывается последняя инструкция в блоке: переменные, которые в ней использованы, считаются живыми.
```csharp
var last = instructions.Last();
newInstructions.Add(last);
varStatus.Add(last.Result, false);
if (!int.TryParse(last.Argument1, out _) && last.Argument1 != "True" && last.Argument1 != "False")
{
    varStatus[last.Argument1] = true;
}
if (!int.TryParse(last.Argument2, out _) && last.Argument2 != "True" && last.Argument2 != "False")
{
    varStatus[last.Argument2] = true;
}
```

Затем выполняется цикл по всем инструкциям, кроме последней, в обратном порядке. Пустые операторы добавляются в новый список инструкций "как есть". Если переменная, которой выполняется присваивание, отмечена в словаре как мёртвая, либо является временной и отсутствует в словаре, то такое присваивание заменяется на пустой оператор.
```csharp
if (varStatus.ContainsKey(instruction.Result) && !varStatus[instruction.Result]
    || instruction.Result.First() == '#' && !varStatus.ContainsKey(instruction.Result))
{
    newInstructions.Add(new Instruction(instruction.Label, "noop", null, null, null));
    wasChanged = true;
    continue;
}
```

Если присваивание не является мёртвым кодом, то переменная, которой выполняется присваивание, отмечается как мёртвая, а переменные, использующиеся в правой части, помечаются как живые, и присваивание добавляется в новый список.
```csharp
varStatus[instruction.Result] = false;
if (!int.TryParse(instruction.Argument1, out _) && instruction.Argument1 != "True" && instruction.Argument1 != "False")
{
    varStatus[instruction.Argument1] = true;
}
if (instruction.Operation != "UNMINUS" && instruction.Operation != "NOT"
    && !int.TryParse(instruction.Argument2, out _) && instruction.Argument2 != "True" && instruction.Argument2 != "False")
{
    varStatus[instruction.Argument2] = true;
}
newInstructions.Add(instruction);
```

После цикла по всем инструкциям новый список инструкций переворачивается и возвращается как результат метода. 

#### Место в общем проекте (Интеграция)
Данная оптимизация является одной из оптимизаций трёхадресного кода и используется в общем оптимизаторе `ThreeAddressCodeOptimizer`.

#### Тесты
В тестах проверяется содержимое списка инструкций после выполнения данной оптимизации. Тесты выполняются для следующих примеров:
```
var a;
a = -a;
a = 1;
```

```
var a;
a = true;
a = !a;
```

```
var a, b, c;
a = 1;
a = 2;
b = 11;
b = 22;
a = 3;
a = b;
c = 1;
a = b + c;
b = -c;
c = 1;
b = a - c;
a = -b;
```
Для последнего теста также проверяется совместная работа данной оптимизации и удаления пустых операторов.

<a name="GotoToGoto"/>

### Устранение переходов к переходам

#### Постановка задачи
Создать оптимизирующий модуль программы устраняющий переходы к переходам.

#### Команда
К. Галицкий, А. Черкашин

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* Трехадресный код

#### Теоретическая часть
В рамках этой задачи необходимо было реализовать оптимизацию устранения переходов к переходам. Если оператор goto ведет на метку, содержащую в goto переход на следующую метку, необходимо протянуть финальную метку до начального goto.
Были поставлены  следующие 3 случая задачи:
* 1 случай 
  До
  ```csharp
  goto L1;
  ...
  L1: goto L2;
  ```
  После
  ```csharp
  goto L2;
  ...
  L1: goto L2;
  ```
* 2 случай
  До
  ```csharp
  if (/*усл*/) goto L1;
  ...
  L1: goto L2;
  ```
  После
  ```csharp
  if (/*усл*/) goto L2;
  ...
  L1: goto L2;
  ```
* 3 случай
  Если есть ровно один переход к L1 и оператору с L1 предшествует безусловный переход
  До
  ```csharp
  goto L1;
  ...
  L1: if (/*усл*/) goto L2;
  L3:
  ```
  После
  ```csharp
  ...
  if (/*усл*/) goto L2;
  goto L3;
  ...
  L3:
  ```

#### Практическая часть
Реализовали метод для удаления переходов к переходам и разделили его на 3 случая:
```csharp
wasChanged = false;
var tmpCommands = new List<Instruction>();
tmpCommands.AddRange(commands.ToArray()); // Перепишем набор наших инструкций в темповый массив

foreach (var instr in commands)
{
	if (instr.Operation == "goto") // Простые goto (для случая 1)
	{
		tmpCommands = StretchTransitions(instr.Argument1, tmpCommands);
	}

	if (instr.Operation == "ifgoto" && instr.Label == "") // Инструкции вида if(усл) goto (для случая 2)
	{
		tmpCommands = StretchIFWithoutLabel(instr.Argument2, tmpCommands);
	}

	if (instr.Operation == "ifgoto" && instr.Label != "") // Инструкции вида l1: if(усл) goto (для случая 3)
	{
		tmpCommands = StretchIFWithLabel(instr, tmpCommands);
	}
}
return (wasChanged, tmpCommands);
```
Реализовали три вспомогательные функции для каждого случая задачи.
Вспомогательная функция для случая 1:
```csharp
/// <summary>
/// Протягивает метки для goto
/// </summary>
/// <param name="Label">Метка которую мы ищем</param>
/// <param name="instructions">Набор наших инструкций</param>
/// <returns>
/// Вернет измененные инструкции с протянутыми goto
/// </returns>
private static List<Instruction> StretchTransitions(string Label, List<Instruction> instructions)
{
	for (int i = 0; i < instructions.Count; i++)
	{
		// Если метка инструкции равна метке которую мы ищем, и на ней стоит оперецаия вида "goto" и метка слева не равна метке справа
		if (instructions[i].Label == Label 
                && instructions[i].Operation == "goto" 
                && instructions[i].Argument1 != Label)
		{
			string tmp = instructions[i].Argument1;
			for (int j = 0; j < instructions.Count; j++)
			{
				//Для всех "goto" с искомой меткой, протягиваем нужный нам Label
				if (instructions[j].Operation == "goto" && instructions[j].Argument1 == Label)
				{
					wasChanged = true;
					instructions[j] = new Instruction(instructions[j].Label, "goto", tmp, "", "");
				}
			}
		}
	}
	return instructions;
}
```
Вспомогательная функция для случая 2:
```csharp
/// <summary>
/// Протягивает метки для if(усл) goto
/// </summary>
/// <param name="Label">Метка которую мы ищем</param>
/// <param name="instructions">Набор наших инструкций</param>
/// <returns>
/// Вернет измененные инструкции с протянутыми goto из if
/// </returns>
private static List<Instruction> StretchIFWithoutLabel(string Label, List<Instruction> instructions)
{
	for (int i = 0; i < instructions.Count; i++)
	{
		// Если метка инструкции равна метке которую мы ищем, и на ней стоит оперецаия вида "goto" и метка слева не равна метке справа
		if (instructions[i].Label == Label 
                && instructions[i].Operation == "goto" 
                && instructions[i].Argument2 != Label)
		{
			string tmp = instructions[i].Argument1;
			for (int j = 0; j < instructions.Count; j++)
			{
				//Для всех "ifgoto" с искомой меткой, протягиваем нужный нам Label
				if (instructions[j].Operation == "ifgoto" && instructions[j].Argument2 == Label)
				{
					wasChanged = true;
					instructions[j] = new Instruction("", "ifgoto", instructions[j].Argument1, tmp, "");
				}
			}
		}
	}
	return instructions;
}
```

Вспомогательная функция для случая 3:
```csharp
private static List<Instruction> StretchIFWithLabel(Instruction findInstruction, List<Instruction> instructions)
{
	int findIndexIf = instructions.IndexOf(findInstruction); //Поиск индекса "ifgoto" на которую существует метка
	
	//проверка на наличие индекса. Проверка на наличие только одного перехода по условию для случая 3
	if (findIndexIf == -1
		|| instructions.Where(x => instructions[findIndexIf].Label == x.Argument1 
                && x.Operation == "goto" 
                && x.ToString() != instructions[findIndexIf].ToString()).Count() > 1)
	{
		return instructions;
	}
	//поиск индекса перехода на требуемый "ifgoto"
	int findIndexGoto = instructions.IndexOf(instructions.Where(x => instructions[findIndexIf].Label == x.Argument1 
                                                                                && x.Operation == "goto").ElementAt(0));

	wasChanged = true;
	
	//Если следущая команда после "ifgoto" не содержит метку
	if (instructions[findIndexIf + 1].Label == "")
	{
		instructions[findIndexGoto] = new Instruction("",
                instructions[findIndexIf].Operation,
                instructions[findIndexIf].Argument1,
                instructions[findIndexIf].Argument2,
                instructions[findIndexIf].Result);
		var tmp = ThreeAddressCodeTmp.GenTmpLabel();
		instructions[findIndexIf] = new Instruction(tmp, "noop", "", "", "");
		instructions.Insert(findIndexGoto + 1, new Instruction("", "goto", tmp, "", ""));
	}
	else //Если следущая команда после "ifgoto" содержит метку
	{
		instructions[findIndexGoto] = new Instruction("",
                instructions[findIndexIf].Operation,
                instructions[findIndexIf].Argument1,
                instructions[findIndexIf].Argument2,
                instructions[findIndexIf].Result);
		var tmp = instructions[findIndexIf + 1].Label;
		instructions[findIndexIf] = new Instruction("", "noop", "", "", "");
		instructions.Insert(findIndexGoto + 1, new Instruction("", "goto", tmp, "", ""));
	}
	return instructions;
}
```

Результатом работы программы является пара значений, была ли применена оптимизация и список инструкций с примененной оптимизацией
```csharp
    return (wasChanged, tmpcommands);
```

#### Место в общем проекте (Интеграция)
Используется после создания трехадресного кода:
```csharp
/* ThreeAddressCodeOptimizer.cs */
private static List<Optimization> BasicBlockOptimizations => new List<Optimization>()
{
    /* ... */
};
private static List<Optimization> AllCodeOptimizations => new List<Optimization>
{
  ThreeAddressCodeGotoToGoto.ReplaceGotoToGoto,
 /* ... */
};

public static List<Instruction> OptimizeAll(List<Instruction> instructions) =>
    Optimize(instructions, BasicBlockOptimizations, AllCodeOptimizations);

/* Main.cs */
var threeAddrCodeVisitor = new ThreeAddrGenVisitor();
parser.root.Visit(threeAddrCodeVisitor);
var threeAddressCode = threeAddrCodeVisitor.Instructions;
var optResult = ThreeAddressCodeOptimizer.OptimizeAll(threeAddressCode);
```

#### Тесты
В тестах проверяется, что применение оптимизации устранения переходов к переходам к заданному трехадресному коду, возвращает ожидаемый результат:
```csharp
[Test]
public void MultiGoToTest()
{
	var TAC = GenTAC(@"
var a, b;
1: goto 2;
2: goto 5;
3: goto 6;
4: a = 1;
5: goto 6;
6: a = b;
");
	var optimizations = new List<Optimization> { ThreeAddressCodeGotoToGoto.ReplaceGotoToGoto };

	var expected = new List<string>()
	{
		"1: goto 6",
		"2: goto 6",
		"3: goto 6",
		"4: a = 1",
		"5: goto 6",
		"6: a = b",
	};
	var actual = ThreeAddressCodeOptimizer.Optimize(TAC, allCodeOptimizations: optimizations)
		.Select(instruction => instruction.ToString());

	CollectionAssert.AreEqual(expected, actual);
}

[Test]
public void TestGotoIfElseTACGen1()
{
	var TAC = GenTAC(@"
var a,b;
b = 5;
if(a > b)
goto 6;
6: a = 4;
");
	var optimizations = new List<Optimization> { ThreeAddressCodeGotoToGoto.ReplaceGotoToGoto };

	var expected = new List<string>()
	{
		"b = 5",
		"#t1 = a > b",
		"if #t1 goto 6",
		"goto L2",
		"L1: goto 6",
		"L2: noop",
		"6: a = 4",
	};
	var actual = ThreeAddressCodeOptimizer.Optimize(TAC, allCodeOptimizations: optimizations)
		.Select(instruction => instruction.ToString());

	CollectionAssert.AreEqual(expected, actual);
}

[Test]
public void GoToLabelTest()
{
	var TAC = GenTAC(@"
var a;
goto 1;
1: goto 2;
2: goto 3;
3: goto 4;
4: a = 4;
");
	var optimizations = new List<Optimization> { ThreeAddressCodeGotoToGoto.ReplaceGotoToGoto };

	var expected = new List<string>()
	{
		"goto 4",
		"1: goto 4",
		"2: goto 4",
		"3: goto 4",
		"4: a = 4",
	};
	var actual = ThreeAddressCodeOptimizer.Optimize(TAC, allCodeOptimizations: optimizations)
		.Select(instruction => instruction.ToString());

	CollectionAssert.AreEqual(expected, actual);
}
```

<a name="BasicBlockLeader"/>

### Разбиение на ББл (от лидера до лидера)

#### Постановка задачи
Реализовать разбиение на базовые блоки от лидера до лидера.

#### Команда
К. Галицкий, А. Черкашин

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* Трехадресный код
* Создание структуры ББл и CFG – графа ББл
Зависимые задачи:
* Def-Use информация и удаление мертвого кода
* Свертка констант
* Учет алгебраических тождеств
* Протяжка констант
* Протяжка копий
* Живые и мертвые переменные и удаление мертвого кода
* Построение CFG. Обход потомков и обход предков для каждого базового блока

#### Теоретическая часть
В рамках этой задачи необходимо было реализовать разбиение трехадресного кода на базовые блоки.
Базовый блок – это блок команд от лидера до лидера.
Команды лидеры:
* первая команда
* любая команда, на которую есть переход
* любая команда, непосредственно следующая за переходом

Пример разбиение трехадресного кода на базовые блоки:

![Разбиение на базовые блоки](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/2_BasicBlockLeader/pic1.jpg)

#### Практическая часть
Реализовали создание списка операций лидеров:
```csharp
List<BasicBlock> basicBlockList = new List<BasicBlock>(); // список ББл
List<Instruction> temp = new List<Instruction>(); // временный список, для хранения трёхадресных команд для текущего ББл
List<int> listOfLeaders = new List<int>(); //Список лидеров
    for (int i = 0; i < instructions.Count; i++) // формируем список лидеров
    {
        if (i == 0) //Первая команда трехадресного кода
        {
            listOfLeaders.Add(i);
        }

        if (instructions[i].Label != null
            && IsLabelAlive(instructions, instructions[i].Label)) //Команда содержит метку, на которую существует переход
        {
            if (!listOfLeaders.Contains(i)) // проверка на наличие данного лидера в списке лидеров
            {
                listOfLeaders.Add(i);
            }
        }

        if (instructions[i].Operation == "goto"
            || instructions[i].Operation == "ifgoto") //Команда является следующей после операции перехода (goto или ifgoto)
        {
            if (!listOfLeaders.Contains(i + 1)) // проверка на наличие данного лидера в списке лидеров
            {
                listOfLeaders.Add(i + 1);
            }
        }
    }
```

Заполнение списка базовых блоков:

```csharp
int j = 0;
for (int i = 0; i < instructions.Count; i++) // заполняем BasicBlock
{   //Заполняем временный список
    temp.Add(new Instruction(instructions[i].Label,
                                instructions[i].Operation,
                                instructions[i].Argument1,
                                instructions[i].Argument2,
                                instructions[i].Result));

    if (i + 1 >= instructions.Count
        || i == listOfLeaders[((j + 1) >= listOfLeaders.Count ? j : j + 1)] - 1) // Следующая команда в списке принадлежит другому лидеру или последняя команда трехадресного кода
    {
        basicBlockList.Add(new BasicBlock(temp)); //Добавляем ББл
        temp = new List<Instruction>(); //Создаем новый пусток список
        j++;
    }
}
```

Результатом работы является список базовых блоков, состоящий из команд трехадресного кода, разбитых от лидера до лидера:
```csharp
	return basicBlockList;
```

#### Место в общем проекте (Интеграция)
Используется после создания трехадресного кода. Необходим для разбиение трехадресного кода на базовые блоки.
```csharp
/* Main.cs */
var threeAddrCodeVisitor = new ThreeAddrGenVisitor();
parser.root.Visit(threeAddrCodeVisitor);
var threeAddressCode = threeAddrCodeVisitor.Instructions;
var optResult = ThreeAddressCodeOptimizer.OptimizeAll(threeAddressCode);
var divResult = BasicBlockLeader.DivideLeaderToLeader(optResult);
```

#### Тесты
В тестах проверяется, что для заданного трехадресного кода разбиение на ББл возвращает ожидаемый результат:
```csharp
[Test]
public void LabelAliveTest()
{
    var TAC = GenTAC(@"
            var a, b, c;
            goto 3;
            a = 54;
            3: b = 11;
            ");


    var expected = new List<BasicBlock>()
            {
                new BasicBlock(new List<Instruction>(){new Instruction("3", "", "", "goto", "")}),
                new BasicBlock(new List<Instruction>(){new Instruction("54", "", "", "assign", "a")}),
                new BasicBlock(new List<Instruction>(){new Instruction("11", "3", "", "assign", "b")}),
            };
    var actual = BasicBlockLeader.DivideLeaderToLeader(TAC);

    AssertSet(expected, actual);
}

[Test]
public void LabelNotAliveTest()
{
    var TAC = GenTAC(@"
            var a, b, c;
            goto 4;
            a = 54;
            3: b = 11;
            ");


    var expected = new List<BasicBlock>()
            {
                new BasicBlock(new List<Instruction>(){new Instruction("4", "", "", "goto", "")}),
                new BasicBlock(new List<Instruction>(){new Instruction("54", "", "", "assign", "a"),
                                new Instruction("11", "3", "", "assign", "b")}),
            };
    var actual = BasicBlockLeader.DivideLeaderToLeader(TAC);

    AssertSet(expected, actual);
}

[Test]
public void OneBlockTest()
{
    var TAC = GenTAC(@"
var a, b, c;
a = 54;
b = 11;
");


    var expected = new List<BasicBlock>()
            {
                new BasicBlock(new List<Instruction>(){new Instruction("54", "", "", "assign", "a"),
                                new Instruction("11", "", "", "assign", "b")}),
            };
    var actual = BasicBlockLeader.DivideLeaderToLeader(TAC);

    AssertSet(expected, actual);
}
```

<a name="ThreeAddressCodeOptimizer"/>

### Интеграция оптимизаций трёхадресного кода между собой
#### Постановка задачи
Необходимо скомбинировать созданные ранее оптимизации трёхадресного кода так, чтобы они могли выполняться все вместе, друг за другом.
#### Команда
Д. Володин, Н. Моздоров
#### Зависимые и предшествующие задачи
Предшествующие: 
- Def-Use информация: накопление информации и удаление мертвого кода на ее основе
- Устранение переходов к переходам
- Очистка кода от пустых операторов
- Устранение переходов через переходы
- Учет алгебраических тождеств
- Живые и мертвые перем и удаление мертвого кода (замена на пустой оператор)
- Оптимизация общих подвыражений
- Протяжка констант
- Протяжка копий
- Разбиение трёхадресного кода на базовые блоки

#### Теоретическая часть
Необходимо организовать выполнение оптимизаций трёхадресного кода до тех пор, пока каждая из созданных оптимизаций перестанет изменять текущий список инструкций.

#### Практическая часть
Для данной задачи был создан статический класс `ThreeAddressCodeOptimizer`, содержащий два публичных метода: `Optimize` и `OptimizeAll`. Первый метод на вход получает список инструкций, а также два списка оптимизаций: те, которые работают в пределах одного базового блока, и те, которые работают для всего кода программы. Параметрам - спискам оптимизаций по умолчанию присвоено значение `null`, что позволяет при вызове указывать только один из списков. Второй метод на вход получает только список инструкций и использует оптимизации, хранящиеся в двух приватных списках внутри класса, содержащих все созданные оптимизации трёхадресного кода.

Оптимизация выполняется следующим образом: сначала список инструкций делится на базовые блоки, затем для каждого блока отдельно выполняются все оптимизации в пределах одного блока, затем инструкции в блоках объединяются и выполняются все глобальные оптимизации. Общая оптимизация в пределах одного блока и общая оптимизация всего кода выполняются похожим образом и представляют собой циклы, пока все соответствующие оптимизации не перестанут изменять список инструкций, и в этих циклах по очереди выполняется каждая из соответствующих оптимизаций. Если какая-то из оптимизаций изменила список инструкций, то выполнение всех оптимизаций происходит заново. Ниже приведён код для общей оптимизации в пределах одного блока.
```csharp
private static BasicBlock OptimizeBlock(BasicBlock block, List<Optimization> opts)
{
    var result = block.GetInstructions();
    var currentOpt = 0;
    while (currentOpt < opts.Count)
    {
        var (wasChanged, instructions) = opts[currentOpt++](result);
        if (wasChanged)
        {
            currentOpt = 0;
            result = instructions;
        }
    }
    return new BasicBlock(result);
}
```

#### Место в общем проекте (Интеграция)
Данная оптимизация объединяет созданные ранее оптимизации трёхадресного кода, и в дальнейшем на основе результата выполнения всех оптимизаций выполняется построение графа потока управления.
#### Тесты
Класс `ThreeAddressCodeOptimizer` используется во всех тестах для проверки оптимизаций трёхадресного кода (в том числе тех оптимизаций, которые дополняют действие друг друга). Схема тестирования выглядит следующим образом: сначала по заданному тексту программы генерируется трёхадресный код, затем задаются списки оптимизаций для проверки, после этого вызывается метод `Optimize` класса `ThreeAddressCodeOptimizer` и сравнивается полученный набор инструкций с ожидаемым набором. Ниже приведён один из тестов. 
```csharp
[Test]
public void VarAssignSimple()
{
    var TAC = GenTAC(@"
var a, b, x;
x = a;
x = b;
");
    var optimizations = new List<Optimization> { ThreeAddressCodeDefUse.DeleteDeadCode };

    var expected = new List<string>()
    {
        "noop",
        "x = b"
    };
    var actual = ThreeAddressCodeOptimizer.Optimize(TAC, optimizations)
        .Select(instruction => instruction.ToString());

    CollectionAssert.AreEqual(expected, actual);
}
```

<a name="LiveVariableAnalysis"/>

### Анализ активных переменных

#### Постановка задачи
Необходимо накопить IN-OUT информацию для дальнейшей оптимизации «Живые и мертвые переменные» между базовыми блоками.

#### Команда
А. Татарова, Т. Шкуро

#### Зависимые и предшествующие задачи
Предшествующие:
- Построение графа потока управления

Зависимые:
- Использование информации IN-OUT в удалении мертвого кода (Живые и мертвые переменные)

#### Теоретическая часть
Для переменной x и точки p анализ выясняет, может ли значение x из точки p использоваться вдоль некоторого пути в графе потока управления, начинающемся в точке p. Если может, то переменная x активна(жива) в точке p, если нет — неактивна(мертва). 
__defB__ – множество переменных, определенных в блоке B до любых их использований в этом блоке.
__useB__ -  множество переменных, значения которых могут использоваться в блоке B до любых определений этих переменных.
Отсюда любая переменная из useB рассматривается как активная на входе в блок B, а переменная из defB рассматривается как мертвая на входе в блок B. 
И тогда множества IN и OUT определяются следующими уравнениями

1. ![Уравнение 1](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_LiveVariableAnalysis/pic1.jpg)

Это уравнение определяет граничное условие, что активных переменных при выходе из программы нет.

1. ![Уравнение 2](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_LiveVariableAnalysis/pic2.jpg)

Второе уравнение говорит о том, что переменная активна при выходе из блока тогда и только тогда, когда она активна при входе по крайней мере в один из дочерних блоков. Здесь оператор сбора является объединением.

1. ![Уравнение 3](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_LiveVariableAnalysis/pic3.jpg)

Здесь уравнение гласит, что переменная активна при входе в блок, если она используется в блоке до переопределения или если она активна на выходе из блока и не переопределена в нем.


Анализ активных переменных идет обратно направлению потока управления, поскольку необходимо проследить, что использование переменной x в точке p передается всем точкам, предшествующим p вдоль путей выполнения. 

#### Практическая часть
Первым шагом для каждого блока строятся def и use множества переменных. 
```csharp
private (HashSet<string> def, HashSet<string> use) FillDefUse(List<Instruction> block)
{
    Func<string, bool> IsId = ThreeAddressCodeDefUse.IsId;
    var def = new HashSet<string>();
    var use = new HashSet<string>();
    for (var i = 0; i < block.Count; ++i)
    {
        var inst = block[i];
        if (IsId(inst.Argument1) && !def.Contains(inst.Argument1))
        {
            use.Add(inst.Argument1);
        }
        if (IsId(inst.Argument2) && !def.Contains(inst.Argument2))
        {
            use.Add(inst.Argument2);
        }
        if (IsId(inst.Result) && !use.Contains(inst.Result))
        {
            def.Add(inst.Result);
        }
    }
    return (def, use);
}
```
где ```IsID``` --- функция определения переменной.
Далее определяется передаточная функция по уравнению (3)
```csharp
public HashSet<string> Transfer(BasicBlock basicBlock, HashSet<string> OUT) =>
    dictDefUse[basicBlock].Use.Union(OUT.Except(dictDefUse[basicBlock].Def)).ToHashSet();
```
где ```dictDefUse``` - структура для хранения def-use для каждого блока, ```OUT``` - множество, вычисленное уже для этого блока.

Сам анализ запускается на графе потока управления и выдает IN-OUT множества для каждого блока графа.
```csharp
public void ExecuteInternal(ControlFlowGraph cfg)
{
    var blocks = cfg.GetCurrentBasicBlocks();
    var transferFunc = new LiveVariableTransferFunc(cfg); //определение передаточной функции
    
    //каждый блок в начале работы алгоритма хранит пустые IN и OUT множества
    //в том числе входной и выходной блоки
    foreach (var x in blocks)
	{
        dictInOut.Add(cfg.VertexOf(x), new InOutSet()); 
    }
    //алгоритм вычисляет до тех пор, пока IN-OUT множества меняются на очередной итерации
    bool isChanged = true;
    while (isChanged)
	{
        isChanged = false;
        for (int i = blocks.Count - 1; i >= 0; --i)
		{
            var children = cfg.GetChildrenBasicBlocks(i);
            //здесь собирается информация IN множеств от дочерних узлов
            dictInOut[i].OUT =
                children
                .Select(x => dictInOut[x.Item1].IN)
                .Aggregate(new HashSet<string>(), (a, b) => a.Union(b).ToHashSet());
            var pred = dictInOut[i].IN;
            //Вычисление IN передаточной функцией
            dictInOut[i].IN = transferFunc.Transfer(blocks[i], dictInOut[i].OUT);
            isChanged = !dictInOut[i].IN.SetEquals(pred) || isChanged;
        }
    }
}
```

#### Место в общем проекте (Интеграция)
Анализ активных переменных является одним из итерационных алгоритмов по графу потока управления, преобразующих глобально текст программы. 
На данный момент анализ представлен как отдельный метод (```ExecuteInternal```) и как реализация абстрактного класса, представляющего собой обобщенный итерационный алгоритм:

```csharp
    public override Func<HashSet<string>, HashSet<string>, HashSet<string>> CollectingOperator =>
        (a, b) => a.Union(b).ToHashSet();
    public override Func<HashSet<string>, HashSet<string>, bool> Compare =>
        (a, b) => a.SetEquals(b);
    public override HashSet<string> Init { get => new HashSet<string>(); protected set { } }
    public override Func<BasicBlock, HashSet<string>, HashSet<string>> TransferFunction 
        { get; protected set; }
    public override Direction Direction => Direction.Backward;
        /*...*/
    public override InOutData<HashSet<string>> Execute(ControlFlowGraph cfg)
    {
        TransferFunction = new LiveVariableTransferFunc(cfg).Transfer;
        return base.Execute(cfg);
    }
```

#### Тесты
В тестах проверяется, что для заданного текста программы (для которого генерируется трехадресный код и граф потока управления по нему) анализ активных переменных возвращает ожидаемые IN-OUT множества для каждого блока:
```csharp
[Test]
public void WithCycle() {
var TAC = GenTAC(@"
var a,b,c;
input (b);
while a > 5{
	a = b + 1;
	c = 5;
}
print (c);"
);
    List<(HashSet<string> IN, HashSet<string> OUT)> expected =
        new List<(HashSet<string> IN, HashSet<string> OUT)>(){
            (new HashSet<string>(){"a","c"}, new HashSet<string>(){"a","c"}),
            (new HashSet<string>(){"a","c"}, new HashSet<string>(){"a","b","c"}),
            (new HashSet<string>(){"a","b","c"}, new HashSet<string>(){"b", "c"}),
            (new HashSet<string>(){ "c" }, new HashSet<string>(){ "c" }),
            (new HashSet<string>(){"b"}, new HashSet<string>(){ "a", "b", "c"}),
            (new HashSet<string>(){"c"}, new HashSet<string>(){ }),
            (new HashSet<string>(){ }, new HashSet<string>(){ })
        };
    var actual = Execute(TAC);
    AssertSet(expected, actual);
}

[Test]
public void ComplexWithCycleTest() {
    var TAC = GenTAC(@"
var a,b,c,i;
for i = 1,b {
	input (a);
	c = c + a;
	print(c);
	if c < b
		c = c + 1;
	else {
		b = b - 1;
		print(b);
		print(c);
	}
}
print (c+a+b);"
);
    List<(HashSet<string> IN, HashSet<string> OUT)> expected =
        new List<(HashSet<string> IN, HashSet<string> OUT)>(){
            (new HashSet<string>(){"b","c","a"}, new HashSet<string>(){"c","b","a"}),
            (new HashSet<string>(){"b","c","a"}, new HashSet<string>(){"c","b","i","a"}),
            (new HashSet<string>(){"c","b","i","a"}, new HashSet<string>(){"c","b","i","a"}),
            (new HashSet<string>(){"c","a","b"}, new HashSet<string>(){"c","a","b"}),
            (new HashSet<string>(){"c","b","i"}, new HashSet<string>(){"c","b","i","a"}),
            (new HashSet<string>(){"c","b","i","a"}, new HashSet<string>(){"c","b","i","a"}),
            (new HashSet<string>(){"c","b","i","a"}, new HashSet<string>(){"c","b","i","a"}),
            (new HashSet<string>(){"c","b","i","a"}, new HashSet<string>(){"c","b","i","a"}),
            (new HashSet<string>(){"c","a","b"}, new HashSet<string>(){ }),
            (new HashSet<string>(){ }, new HashSet<string>(){ })
        };
    var actual = Execute(TAC);
    AssertSet(expected, actual);
}
```
<a name="ReachingDefinitions"/>

### Анализ достигающих определений
#### Постановка задачи
Необходимо накопить IN-OUT информацию по достигающим определениям в базовых блоках для дальнейшей оптимизации.
#### Команда
Д. Володин, Н. Моздоров
#### Зависимые и предшествующие задачи
Предшествующие: 
- Построение графа потока управления
- Вычисление передаточной функции для достигающих определений
- Итерационный алгоритм в обобщённой структуре

#### Теоретическая часть
Определение *d* достигает точки *p*, если существует путь от точки, непосредственно следующей за *d*, к точке *p*, такой, что *d* не уничтожается вдоль этого пути. Обозначим *genB* --- множество определений, генерируемых и не переопределённых базовым блоком *B*, и *killB* --- множество остальных определений переменных, определяемых в определениях *genB*, в других базовых блоках. Тогда решить задачу о достигающих определениях можно с помощью итерационного алгоритма: на вход ему подаётся граф потока управления с вычисленными для каждого базового блока множествами *genB* и *killB*, описание алгоритма представлено ниже.

![Алгоритм решения задачи о достигающих определениях](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_ReachingDefinitions/pic1.jpg)

На каждом шаге IN[*B*] и OUT[*B*] не уменьшаются для всех *B* и ограничены сверху, поэтому алгоритм сходится.

#### Практическая часть
Для решения задачи использовался обобщённый итерационный алгоритм. Свойства, использующиеся в нём, задаются следующим образом:
```csharp
/// оператор сбора
 public override Func<IEnumerable<BasicBlock>, IEnumerable<BasicBlock>, IEnumerable<BasicBlock>> CollectingOperator
     => (a, b) => a.Union(b);

/// оператор сравнения (условие продолжения цикла)
public override Func<IEnumerable<BasicBlock>, IEnumerable<BasicBlock>, bool> Compare
    => (a, b) => !a.Except(b).Any() && !b.Except(a).Any();
    
/// Начальное значение для всех блоков, кроме первого
public override IEnumerable<Instruction> Init { get => Enumerable.Empty<Instruction>(); protected set { } }
    
/// передаточная функция
public override Func<BasicBlock, IEnumerable<Instruction>, IEnumerable<Instruction>> TransferFunction { get; protected set; }
```

Свойство ```TransferFunction```, задающее передаточную функцию, зависит от графа потока управления, и она задаётся во время вызова алгоритма:
```csharp
TransferFunction = new ReachingTransferFunc(graph).Transfer;
```
Результат возвращается методом ```Execute```.

#### Место в общем проекте (Интеграция)
Анализ достигающих определений является одним из итерационных алгоритмов по графу потока управления, анализирующих глобально текст программы. Его реализация основана на обобщённом итерационном алгоритме, реализованном другой командой. Передаточная функция для достигающих определений была изначально реализована в рамках текущей задачи, а затем вынесена как отдельная задача.
#### Тесты
В тестах проверяется содержимое IN и OUT для каждого базового блока программы. Тестирование проводится на различных примерах: 
- из одного базового блока
```
var a, b, c;
b = 3;
a = 1;
a = 2;
b = a;
c = a;
c = b;
```
- с ветвлениями
```
var a, b;
input(a);
if a > 0
    b = 0;
else
    a = 1;
b = a;
```
- с циклами
```
var i, k;
for k = 0, 2
    i = i + 1;
```
- комбинированные тесты
```
var i, m, j, n, a, u1, u2, u3, k;
1: i = m - 1;
2: j = n;
3: a = u1;
for k = 0, 1
{
    i = i + 1;
    j = j - 1;

    if i < j
        a = u2;
    i = u3;
}
```

<a name="GenericIterativeAlgorithm"/>

### Итерационный алгоритм в обобщённой структуре

#### Постановка задачи
Реализовать итеративный алгоритм в обобщенной структуре.

#### Команда
К. Галицкий, А. Черкашин

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* Построение CFG. Обход потомков и обход предков для каждого ББл
Зависимые задачи:
* Вычисление передаточной функции для достигающих определений композицией передаточных функций команд
* Активные переменные
* Доступные выражения
* Передаточная функция в структуре распространения констант
* Достигающие определения
* Итерационный алгоритм в структуре распространения констант


#### Теоретическая часть
В рамках этой задачи необходимо было реализовать обобщенный итерационный алгоритм.
Входы итерационного алгоритма:
* Граф потока данных с помеченными  входными и выходными узлами
* Направление потока данных
* Множество значений V
* Оператор сбора ∧
* Множество функций f где f(b) из F представляет собой передаточную функцию для блока b
* Константное значение v вход или v выход из V, представляющее собой граничное условие для прямой и обратной структуры соответсвтвенно.
Выходы итерационного алгоритма:
* Значения из V для in(b) и out(b) для каждого блока b в CFG


Алгоритм для решения прямой задачи потока данных:

![Прямая задача потока данных](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_GenericIterativeAlgorithm/pic2.JPG)

Алгоритм для решения обратной задачи потока данных:

![Обратная задача потока данных](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_GenericIterativeAlgorithm/pic1.JPG)

Служит для избежания базового итеративного алгоритма для каждой структуры потока данных используемой на стадии оптимизации.
Его задача вычисление in и out для каждого блока как ряд последовательных приближений. А так же его использование предоставляет ряд полезных свойств приведенных ниже:

![Свойства алгоритма](https://github.com/Taally/FIIT_6_compiler/blob/feature/try-doc-gen/Documentation/3_GenericIterativeAlgorithm/pic3.JPG)

#### Практическая часть
Реализовали класс выходных данных:
```csharp
public class InOutData<T> : Dictionary<BasicBlock, (T In, T Out)> // Вид выходных данных вида (Базовый блок, (его входы, его выходы))
        where T : IEnumerable
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("++++");
            foreach (var kv in this)
            {
                sb.AppendLine(kv.Key + ":\n" + kv.Value);
            }
            sb.AppendLine("++++");
            return sb.ToString();
        }

        public InOutData() { }  // конструктор по умолчанию

        public InOutData(Dictionary<BasicBlock, (T, T)> dictionary)  // Конструктор заполнения выходных данных
        {
            foreach (var b in dictionary)
            {
                this[b.Key] = b.Value;
            }
        }
    }
```

Указываем вид прохода алгоритма:
```csharp
public enum Direction { Forward, Backward }
```

Реализовали алгоритм:
```csharp
public abstract class GenericIterativeAlgorithm<T> where T : IEnumerable
    {
        /// <summary>
        /// Оператор сбора
        /// </summary>
        public abstract Func<T, T, T> CollectingOperator { get; }

        /// <summary>
        /// Сравнение двух последовательностей (условие продолжения цикла)
        /// </summary>
        public abstract Func<T, T, bool> Compare { get; }

        /// <summary>
        /// Начальное значение для всех блоков, кроме первого
        /// (при движении с конца - кроме последнего)
        /// </summary>
        public abstract T Init { get; protected set; }

        /// <summary>
        /// Начальное значение для первого блока
        /// (при движении с конца - для последнего)
        /// </summary>
        public virtual T InitFirst { get => Init; protected set { } }

        /// <summary>
        /// Передаточная функция
        /// </summary>
        public abstract Func<BasicBlock, T, T> TransferFunction { get; protected set; }

        /// <summary>
        /// Направление
        /// </summary>
        public virtual Direction Direction => Direction.Forward;

        /// <summary>
        /// Выполнить алгоритм
        /// </summary>
        /// <param name="graph"> Граф потока управления </param>
        /// <returns></returns>
        public virtual InOutData<T> Execute(ControlFlowGraph graph)
        {
            GetInitData(graph, out var blocks, out var data,
                out var getPreviousBlocks, out var getDataValue, out var combine);  
                // Заполнение первого элемента верхним или нижним элементом полурешетки в зависимости от прохода

            var outChanged = true;  // Были ли внесены изменения
            while (outChanged)
            {
                outChanged = false;
                foreach (var block in blocks)  //  цикл по блокам
                {
                    var inset = getPreviousBlocks(block).Aggregate(Init, (x, y) => CollectingOperator(x, getDataValue(y)));  // Применение оператора сбора для всей колекции
                    var outset = TransferFunction(block, inset);  // применение передаточной функции

                    if (!Compare(outset, getDataValue(block)))  // Сравнение на равенство множеств методом пересечения
                    {
                        outChanged = true;
                    }
                    data[block] = combine(inset, outset);  // Запись выходных данных
                }
            }
            return data;
        }

        private void GetInitData(  // функция инициализации данных относительно вида прохода алгоритма
            ControlFlowGraph graph,
            out IEnumerable<BasicBlock> blocks,
            out InOutData<T> data,
            out Func<BasicBlock, IEnumerable<BasicBlock>> getPreviousBlocks,
            out Func<BasicBlock, T> getDataValue,
            out Func<T, T, (T, T)> combine)
        {
            var start = Direction == Direction.Backward  // Если обратный проход то мы берем блоки с конца, в обратном случае с начала
                ? graph.GetCurrentBasicBlocks().Last()
                : graph.GetCurrentBasicBlocks().First();
            blocks = graph.GetCurrentBasicBlocks().Except(new[] { start }); 

            var dataTemp = new InOutData<T>
            {
                [start] = (InitFirst, InitFirst)  // инициализация первого элемента полурешетки
            };
            foreach (var block in blocks)
            {
                dataTemp[block] = (Init, Init); // инициализация остальных элементов полурешетки
            }
            data = dataTemp;

            switch (Direction) // инициализация свойств для каждого типа прохода
            {
                case Direction.Forward:
                    getPreviousBlocks = x => graph.GetParentsBasicBlocks(graph.VertexOf(x)).Select(z => z.Item2);
                    getDataValue = x => dataTemp[x].Out;
                    combine = (x, y) => (x, y);
                    break;
                case Direction.Backward:
                    getPreviousBlocks = x => graph.GetChildrenBasicBlocks(graph.VertexOf(x)).Select(z => z.Item2);
                    getDataValue = x => dataTemp[x].In;
                    combine = (x, y) => (y, x);
                    break;
                default:
                    throw new NotImplementedException("Undefined direction type");
            }
        }
    }
```

#### Место в общем проекте (Интеграция)
Используется для вызова итерационных алгоритмов в единой структуре.
```csharp

            /* ... */
            var iterativeAlgorithm = new GenericIterativeAlgorithm<IEnumerable<Instruction>>();
            return iterativeAlgorithm.Analyze(graph, new Operation(), new ReachingTransferFunc(graph));
            /* ... */
            /* ... */
            var iterativeAlgorithm = new GenericIterativeAlgorithm<HashSet<string>>(Pass.Backward);
           return iterativeAlgorithm.Analyze(cfg, new Operation(), new LiveVariableTransferFunc(cfg));
           /* ... */

```

#### Тесты
В тестах проверяется использование итерационных алгоритмов в обобщенной структуре, результаты совпадают с ожидаемыми.
```csharp
public void LiveVariableIterativeTest()
        {
            var TAC = GenTAC(@"
var a,b,c;

input (b);
a = b + 1;
if a < c
	c = b - a;
else
	c = b + a;
print (c);"
);

            var cfg = new ControlFlowGraph(BasicBlockLeader.DivideLeaderToLeader(TAC));
            var activeVariable = new LiveVariableAnalysis();
            var resActiveVariable = activeVariable.Execute(cfg);
            HashSet<string> In = new HashSet<string>();
            HashSet<string> Out = new HashSet<string>();
            List<(HashSet<string> IN, HashSet<string> OUT)> actual = new List<(HashSet<string> IN, HashSet<string> OUT)>();
            foreach (var x in resActiveVariable)
            {
                foreach (var y in x.Value.In)
                {
                    In.Add(y);
                }

                foreach (var y in x.Value.Out)
                {
                    Out.Add(y);
                }
                actual.Add((new HashSet<string>(In), new HashSet<string>(Out)));
                In.Clear(); Out.Clear();
            }

            List<(HashSet<string> IN, HashSet<string> OUT)> expected =
                new List<(HashSet<string> IN, HashSet<string> OUT)>()
                {
                    (new HashSet<string>(){"c"}, new HashSet<string>(){ "c" }),
                    (new HashSet<string>(){"c"}, new HashSet<string>(){"a", "b"}),
                    (new HashSet<string>(){"a", "b"}, new HashSet<string>(){ "c" }),
                    (new HashSet<string>(){"a", "b"}, new HashSet<string>(){"c"}),
                    (new HashSet<string>(){"c"}, new HashSet<string>(){ }),
                    (new HashSet<string>(){ }, new HashSet<string>(){ })
                };

            AssertSet(expected, actual);
        }
```

<a name="DominatorTree"/>

### Построение дерева доминаторов
#### Постановка задачи
Необходимо по ранее созданному графу потока управления программы определить множество доминаторов для каждого базового блока, и на основе этой информации построить дерево доминаторов.
#### Команда
Д. Володин, Н. Моздоров
#### Зависимые и предшествующие задачи
Предшествующие: 
- Построение графа потока управления
- Итерационный алгоритм в обобщённой структуре

Зависимые:
- Обратные рёбра и определение того, что CFG является приводимым 
#### Теоретическая часть
Говорят, что базовый блок *b* доминирует над базовым блоком *d*, если любой путь от входного узла в графе потока управления от входного узла к узлу *d* проходит через узел *b*. Множество доминаторов для базовых блоков программы можно найти с помощью итерационного алгоритма: множество доминаторов узла (кроме него самого) – это пересечение доминаторов всех его предшественников, оператором сбора является пересечение множеств, а передаточная функция *f_B*(*x*) = *x* ∪ {*B*}.

Отношение доминирования обладает свойством, что для любого базового блока его доминаторы образуют линейно упорядоченное множество по данному отношению. Нетрудно увидеть, что такое упорядоченное множество представляет собой путь в дереве доминаторов от корня (входного узла) к данному узлу. Анализируя эти пути, можно легко построить дерево доминаторов в графе потока управления.

#### Практическая часть
Для нахождения доминаторов использовался обобщённый итерационный алгоритм, созданный ранее. Свойства, использующиеся в нём, задаются следующим образом:
```csharp
/// оператор сбора
 public override Func<IEnumerable<BasicBlock>, IEnumerable<BasicBlock>, IEnumerable<BasicBlock>> CollectingOperator
     => (x, y) => x.Intersect(y);

/// оператор сравнения (условие продолжения цикла)
public override Func<IEnumerable<BasicBlock>, IEnumerable<BasicBlock>, bool> Compare
    => (x, y) => !x.Except(y).Any() && !y.Except(x).Any();
    
/// передаточная функция
public override Func<BasicBlock, IEnumerable<BasicBlock>, IEnumerable<BasicBlock>> TransferFunction
{
    get => (block, blockList) => blockList.Union(new[] { block });
    protected set { }
}
```

Свойства ```Init``` и ```InitFirst``` зависят от графа потока управления, и они задаются во время вызова алгоритма:
```csharp
Init = graph.GetCurrentBasicBlocks();
InitFirst = graph.GetCurrentBasicBlocks().Take(1);
```
Метод ```GetDominators``` возвращает словарь, в котором ключом является базовый блок, а значением --- соответствующее OUT-множество из итерационного алгоритма.

Построение дерева доминаторов основано на том наблюдении, что, поскольку входной базовый блок доминирует над всеми остальными, базовые блоки с одинаковым количеством доминаторов будут находиться на одном слое в дереве доминаторов. Поэтому можно построить это дерево по слоям, отсортировав узлы по количеству доминаторов, и соединяя каждый последующий базовый блок с тем блоком из предыдущего слоя, который доминирует над данным. 
```csharp
var treeLayers = GetDominators(graph)
    .Where(z => z.Key != start)
    .GroupBy(z => z.Value.Count())
    .OrderBy(z => z.Key);
var tree = new Tree(start);
var prevLayer = new List<BasicBlock>(new[] { start });
foreach (var layer in treeLayers)
{
    var currLayer = layer.ToDictionary(z => z.Key, z => z.Value);
    foreach (var block in currLayer)
    {
        var parent = prevLayer.Single(z => block.Value.Contains(z));
        tree.AddNode(block.Key, parent);
    }
    prevLayer = currLayer.Keys.ToList();
}
return tree;
```
#### Место в общем проекте (Интеграция)
При построении дерева доминаторов используется обобщённый итерационный алгоритм, созданный ранее для задач анализа достигающих определений, активных переменных и доступных выражений. Особенность текущего алгоритма состоит в том, что OUT-множество для входного блока инициализируется не так, как для всех остальных блоков, поэтому в обобщённый алгоритм было добавлено свойство ```InitFirst``` для инициализации OUT-множества для входного блока.
#### Тесты
В тестах проверяется как построение множества доминаторов для каждого базового блока программы, так и построение дерева доминаторов. Тестирование проводится на следующих примерах:
```
var a;
```

```
var a;
a = 1;
```

```
var a, b, c, d, e, f;
1: a = 1;
b = 2;
goto 2;
2: c = 3;
d = 4;
goto 3;
3: e = 5;
goto 4;
4: f = 6;
```

```
var a;
input(a);
1: if a == 0
    goto 2;
a = 2;
2: a = 3;
```

```
var a;
input(a);
1: if a == 0
    goto 2;
if a == 1
    goto 2;
a = 2;
2: a = 3;
```

```
var a, b, c;
input(a);
b = a * a;
if b == 25
    c = 0;
else
    c = 1;
```

```
var a, b, i;
input(a);
for i = 1, 10
{
    b = b + a * a;
    a = a + 1;
}
```
<a name="naturalLoop"/>

### Определение всех естественных циклов

#### Постановка задачи
Необходимо реализовать определение всех естественных циклов программы с использованием обратных ребр.

#### Команда
К. Галицкий, А. Черкашин

#### Зависимые и предшествующие задачи
Предшествующие задачи:
* Обратные рёбра и определение того, что CFG является приводимым
* Построение CFG. Обход потомков и обход предков для каждого ББл

Зависимые задачи
* Построение областей

#### Теоретическая часть
В рамках этой задачи необходимо было реализовать определение всех естественных циклов.
Циклы в исходной программе могут определятся различными способами: как циклы for, while или же они могут быть определены с использованием меток и инструкций goto. С точки зрения анализа программ, не имеет значения, как именно выглядят циклы в исходном тексте программы. Важно только то, что они обладают свойствами, допускающими простую их оптимизацию. В данном случае, нас интересует, имеется ли у цикла одна точка входа, если это так, то компилятор в ходе анализа может предпологать выполнение некоторых начальных условий, в начале каждой итерации цикла. Эта возможность служит причиной определения "естественного цикла".

такие циклы обладают двумя важными свойствами:
* Цикл должен иметь единственный входной узел, называемый заголовком.
* Должно существовать обратное ребро, ведущее в заголовок цикла. В противном случае поток управления не сможет вернуться в заголовок непосредственно из "цикла", т.е. даная структура циклом в таком случае не является.

Вход алгоритма построения естественного цикла обратной дуги:
* Граф потока G и обратная дуга n -> d.
Выход:
* Множество loop, состоящее из всех узлов естественного цикла n -> d.


#### Практическая часть
Реализовали метод возвращающий все естественные циклы:
```csharp
public class NaturalLoop
    {
        /// <summary>
        /// Принимает Граф потока данных и по нему ищет все естественные циклы
        /// </summary>
        /// <param name="cfg">Граф потока управления</param>
        /// <returns>
        /// Вернет все натуральные циклы
        /// </returns>
        public static List<List<BasicBlock>> GetAllNaturalLoops(ControlFlowGraph cfg) // принимаем граф потока данных
        {
            var allEdges = new BackEdges(cfg); // получаем обратные ребра графа
            if (allEdges.GraphIsReducible)  // Проверка графа на приводимость
            {
                var natLoops = new List<List<BasicBlock>>(); // список всех циклов

                var ForwardEdges = cfg.GetCurrentBasicBlocks(); // получаем вершины графа потока управления

                foreach (var (From, To) in allEdges.BackEdgesFromGraph) // проход по всем обратным ребрам
                {
                    if (cfg.VertexOf(To) > 0) // проверка на наличие цикла
                    {
                        var tmp = new List<BasicBlock>(); // временный список
                        for (var i = cfg.VertexOf(To); i < cfg.VertexOf(From) + 1; i++)
                        {
                            if (!tmp.Contains(ForwardEdges[i])) // содержит ли список данный ББл
                            {
                                tmp.Add(ForwardEdges[i]);
                            }
                        }

                        natLoops.Add(tmp); // Добавляем все циклы 
                    }
                }
                // Возвращаем только те циклы, которые являются естественными
                return natLoops.Where(loop => IsNaturalLoop(loop, cfg)).ToList();
              }
              else
              {
                  Console.WriteLine("Граф не приводим");
                  return new List<List<BasicBlock>>();  // Если он не приводим, алгоритм не может работать
              }
        }
```

Вспомогательный метод для проверки циклов на естественность:
```csharp
/// <summary>
/// Проверка цикла на естественность
/// </summary>
/// <param name="loop">Проверяемый цикл</param>
/// <param name="cfg">Граф потока управления</param>
/// <returns>
/// Вернет флаг, естественен ли он
/// </returns>
private static bool IsNaturalLoop(List<BasicBlock> loop, ControlFlowGraph cfg) // принимает цикл и граф потока управления
{
    for (var i = 1; i < loop.Count; i++)
    {
        var parents = cfg.GetParentsBasicBlocks(cfg.VertexOf(loop[i]));// получаем i ББл данного цикла
        // если кол-во родителей больше 1, значит есть вероятность, что цикл содержит метку с переходом извне
        if (parents.Count > 1)  
        {
            foreach (var parent in parents.Select(x => x.block)) // проверяем каждого родителя
            {   // если родитель не принадлежит текущему циклу, этот цикл не является естественным
                if (!loop.Contains(parent))
                {
                    return false;
                }
            }
        }
    }

    return true;
}
```

Результат работы алгоритма :
```csharp
// Возвращаем только те циклы, которые являются естественными
return natLoops.Where(loop => IsNaturalLoop(loop, cfg)).ToList();
```

#### Место в общем проекте (Интеграция)
Используется для вызова итерационных алгоритмов в единой структуре.
```csharp
            /* ... */
            var natcyc = NaturalLoop.GetAllNaturalLoops(cfg);
           /* ... */
```

#### Тесты
В тестах проверяется определение всех естественных циклов.
```csharp
{
    [TestFixture]
    internal class NaturalLoopTest : TACTestsBase
    {
        [Test]
        public void IntersectLoopsTest()
        {
            var TAC = GenTAC(@"
var a, b;

54: a = 5;
55: a = 6;
b = 6;
goto 54;
goto 55;
");

            var cfg = new ControlFlowGraph(BasicBlockLeader.DivideLeaderToLeader(TAC));
            var actual = NaturalLoop.GetAllNaturalLoops(cfg);
            var expected = new List<List<BasicBlock>>()
            {
                new List<BasicBlock>()
                {
                    new BasicBlock(new List<Instruction>(){ TAC[1], TAC[2], TAC[3] }),
                    new BasicBlock(new List<Instruction>(){ TAC[4] })
                }
            };

            AssertSet(expected, actual);
        }

        [Test]
        public void NestedLoopsTest()
        {
            var TAC = GenTAC(@"
var a, b;

54: a = 5;
55: a = 6;
b = 6;
goto 55;
goto 54;

");

            var cfg = new ControlFlowGraph(BasicBlockLeader.DivideLeaderToLeader(TAC));
            var actual = NaturalLoop.GetAllNaturalLoops(cfg);
            var expected = new List<List<BasicBlock>>()
            {
                new List<BasicBlock>()
                {
                    new BasicBlock(new List<Instruction>(){ TAC[1], TAC[2], TAC[3] })
                },
                new List<BasicBlock>()
                {
                    new BasicBlock(new List<Instruction>(){ TAC[0] }),
                    new BasicBlock(new List<Instruction>(){ TAC[1], TAC[2], TAC[3] }),
                    new BasicBlock(new List<Instruction>(){ TAC[4] })
                },


            };

            AssertSet(expected, actual);
        }

        [Test]
        public void OneRootLoopsTest()
        {
            var TAC = GenTAC(@"
var a, b;

54: a = 5;
b = 6;
goto 54;
goto 54;

");

            var cfg = new ControlFlowGraph(BasicBlockLeader.DivideLeaderToLeader(TAC));
            var actual = NaturalLoop.GetAllNaturalLoops(cfg);
            var expected = new List<List<BasicBlock>>()
            {
                new List<BasicBlock>()
                {
                    new BasicBlock(new List<Instruction>(){ TAC[0], TAC[1], TAC[2] })
                },


                new List<BasicBlock>()
                {
                    new BasicBlock(new List<Instruction>(){ TAC[0], TAC[1], TAC[2] }),
                    new BasicBlock(new List<Instruction>(){ TAC[3] })
                }
            };

            AssertSet(expected, actual);
        }

        private void AssertSet(
            List<List<BasicBlock>> expected,
            List<List<BasicBlock>> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                for (var j = 0; j < expected[i].Count; j++)
                {
                    var e = expected[i][j].GetInstructions();
                    var a = actual[i][j].GetInstructions();

                    Assert.AreEqual(a.Count, e.Count);

                    foreach (var pair in a.Zip(e, (x, y) => (actual: x, expected: y)))
                    {
                        Assert.AreEqual(pair.actual.ToString(), pair.expected.ToString());
                    }
                }
            }
        }
    }
}
```
