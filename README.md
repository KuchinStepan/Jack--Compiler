# Описание языка Jack

## Формальная грамматика
Каждый файл программы на Jack — это описание одного класса:
```
class ClassName {
  Variable_declaration*    // поля
  Subroutine_declaration*  // конструкторы, методы, функции в любом порядке
}
```
#### Subroutine_declaration — подпрограммы:
```
SubroutineKeyword Type Name ( Type ParamName , Type ParamName2 , ... ) {
  Local_variable_declaration*
  Statement*
}
...
```

#### SubroutineKeyword — одно из трёх ключевых слов:

 - `constructor` для определения конструкторов;
 - `method` для определения методов объекта;
 - `function` для определения функций (статических методов).

#### Field_variable_declaration — определение полей класса:
```
field Type VarName1 , VarName2 , ... ;
```

#### Static_variable_declaration — определение статических полей класса:
```
static Type VarName1 , VarName2 , ... ;
```

#### Local_variable_declaration — определение локальных переменных внутри метода:
```
var Type VarName1 , VarName2 , ... ;
```

#### Type — три примитивных типа и классы:
```
int | char | boolean | ClassName
```

#### Statement — несколько видов:
```
let VarName = Expression ; // присваивание значения переменной

let VarName [ Expression ] = Expression ; // присваивание значения элементу массива
    
if ( Expression ) {
  Statement*
} else {
  Statement*
}

while ( Expression ) {
  Statement*
}

do Subroutine_call ;  // вызов подпрограммы, результат которой нам не нужен

return Expression ;

return ;
```

#### Expression — то, что можно вычислить и получить значение. Есть следующие способы составлять выражения:
```
Constant

VarName

this

VarName [ Expression ]    // доступ к элементу массива 

Subroutine_call           // подпрограмма должна возвращать значение

- Expression              // унарный минус

~ Expression              // логическое NOT

Expression op Expression  // op — бинарная операция + - * / & | < > = 

( Expression )            // приоритетов операций в Jack нет, 
                          // но можно использовать скобки для группировки
```

#### Constant — натуральные числа, true, false, null, или строки, ограниченные двойными кавычками.

#### Subroutine_call — есть следующие способы вызывать подпрограммы:
```
// вызов функции или создание объекта конструктором:
ClassName.FunctionName ( Expression , Expression , … , Expression )

// вызов метода у объекта varName
VarName.MethodName ( Expression , Expression , … , Expression)
    
// вызов метода у объекта this
MethodName ( Expression , Expression , … , Expression )
```

## Операционная система
Операционная система содержит следующие классы, которыми могут пользоваться все программисты на Jack:

- Math — часто используемые математические функции
- String — работа со строками
- Array — работа с массивами
- Output — вывод текста на экран
- Screen — вывод графики на экран
- Keyboard — ввод с клавиатуры
- Memory — прямой доступ к памяти
- Sys — управление выполнением программы