# LibLinearcs
This is an a port of LibLinear (v 2.21) to C# (DotNetCore)

The project site of the original `C++` version is located at
       http://www.csie.ntu.edu.tw/~cjlin/liblinear/

The upstream changelog can be found at
       http://www.csie.ntu.edu.tw/~cjlin/liblinear/log

The upstream GitHub project can be found at
       https://github.com/cjlin1/liblinear
       
Some changes have been made to the C++ code to ensure performance in C# is acceptable. The largest change was to factor out the sparsematrix implementation.  The code is very much still in development but is fully functioning and performs well.

Whilst these changes may make it slightly more challenging to review the code against the orginal C++ it should be relatively straightforward.

-------------------------------------------------------------------------------

LIBLINEAR is a simple package for solving large-scale regularized linear
classification and regression. It currently supports
- L2-regularized logistic regression/L2-loss support vector classification/L1-loss support vector classification
- L1-regularized L2-loss support vector classification/L1-regularized logistic regression
- L2-regularized L2-loss support vector regression/L1-loss support vector regression.

Please see the readme on the orginal project for more details on LibLinear https://github.com/cjlin1/liblinear

-------------------------------------------------------------------------------

Code Dependencies
- NLog (https://nlog-project.org/) 
