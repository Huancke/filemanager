   M S B A M L   `   `   `  ���� N  JFileManager beta0.1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:FileManagerFileManager  =#clr-namespace:FileManager.ViewModelFileManager.ViewModel      FileManager.MainWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     +localclr-namespace:FileManager     2vm#clr-namespace:FileManager.ViewModel       � Title$  文件管理器��$	��450��$	��800�� � WindowStartupLocation$ CenterScreen=� � 	Resources     BooleanToVisibilityConverter��%   &	        �� .���  � RowDefinitions �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $��* �.+��� $��0�����#007ACCy��� *IsStatusVisible+ �� 	Converter# [  ._�� .+��� 
�������$
��10,5q�$��Center=����� *StatusMessage+�� -   $��×��$��Right=�
������ $��0q�
�������$	��5,0q�$	��5,0q��� $��1�� �� Orientation$ 
Horizontal=�$��5q�.+��� -    ��Name$ 
BackButton��$	��←��$��30��$��0,0,5,0q��� $��删除��$��60��$��0,0,5,0q����� *DeleteCommand+����  �� ElementName$ FileListView�� �� Path$ SelectedItems��� -   	 ��Name$	 PathTextBox��$��2��$��5q�$��3q����� *CurrentPath+�� -   
 ��Name$
 FileListView��$��3��$��5q� �� SelectionMode$ Extended=����� *	FileItems+ �� View �� .P��� $��名称�� �� Width$	 200�� �� DisplayMemberBinding �� *Name+�� $��类型��$	 100�� �� *Type+�� $��大小��$	 120�� �� *Size+�� $��系统文件��$ 80�� �� CellTemplate �� .R��� 
���  ��$	��⚠��y��� *IsSystemFile+# [  