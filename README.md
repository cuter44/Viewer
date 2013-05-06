# Instruction
=======
Assignment of C# Programming of SCAU. A light-weight and function-less image viewer implements with WPF.

rt, 就是一个 C# + WPF 写的渣渣图片浏览器, 用来应付课程作业的. 由于是用VS以及内建库写出来的东东所以支持的解码格式什么的就不用指望了.  
虽然是这么说但是用起来还蛮好的说, 打算继续维护然后替代Win7的原生图片查看器.  

设计上使用仿 PS 的快捷键和仿网页Widget的界面设计

所以也欢迎各位fork或者issue啦. 直接抄去当作业交我也无所谓啦, 给我报告bug就好了.

.Net Framework 4 以上必须, 暂不打算放 binary

# Usage
======
命令行参数调用
`Usage: Viewer image_file_name`

# GUI Manipulation
======
模式制, 初始为无模式, 通过以下按键切换
<blockquote>
[V] -> 移动模式(moVe)
[R] -> 旋转模式(Rotate)
[S] -> 缩放模式(Scale)
{以上模式} + [ESC] -> 无模式
{无模式} + [ESC] -> 退出程序
</blockquote>
相应的 OSD 因为赶工的关系尚未编码

## 移动模式
* 使用鼠标左键拖拽图片

## 旋转模式
* 使用鼠标滚轮旋转图片, 以鼠标光标为旋转中心

## 缩放模式
* 使用鼠标滚轮缩放图片, 以鼠标光标为缩放中心
