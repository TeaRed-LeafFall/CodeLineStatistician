using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CodeLineStatistician
{
    public partial class StatisticianToolWindowControl : UserControl
    {
        DTE2 dte;
        List<string> extensionsList;

        public bool IsCodeFileExtension(string extension)
        {
            return extensionsList.Contains(extension.ToLowerInvariant());
        }
        public StatisticianToolWindowControl()
        {
            this.InitializeComponent();
            dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        private void WalkProjectItems(ProjectItems projectItems, List<Project> projects)
        {
            
            foreach (ProjectItem projectItem in projectItems)
            {
                if (projectItem.SubProject != null)
                {
                    if (string.IsNullOrEmpty(projectItem.Name)) continue;
                    projects.Add(projectItem.SubProject);
                    if (projectItem.SubProject.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        WalkProjectItems(projectItem.SubProject.ProjectItems, projects);
                    }
                }
            }
        }

        public void RefreshProjectList()
        {
            StatisticianButton.IsEnabled = false;
            ResultTextBlock.Text = "";
            // vs获取解决方案中所有项目
            Solution solution = dte.Solution;
            List<Project> projects = new List<Project>();
            foreach (Project project in solution.Projects)
            {
                if (string.IsNullOrEmpty(project.Name)) continue;
                // 添加到项目列表
                projects.Add(project);

                // 如果项目是解决方案文件夹，递归添加子项目
                if (project.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
                {
                    WalkProjectItems(project.ProjectItems, projects);
                }
            }
            // 更新项目列表控件
            ProjectComboBox.ItemsSource = projects;
            ProjectComboBox.DisplayMemberPath = "Name";
            if(projects.Count > 0) {
                ProjectComboBox.SelectedIndex = 0;
                StatisticianButton.IsEnabled = true;
            }
        }

        private List<string> TraverseProjectItems(ProjectItems projectItems)
        {
            var files = new List<string>();
            foreach (ProjectItem projectItem in projectItems)
            {
                // 如果项目项是文件
                if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                {
                    string filePath = projectItem.get_FileNames(1);
                    if (IsCodeFileExtension(System.IO.Path.GetExtension(filePath)))
                    { 
                        files.Add(filePath);
                    }
                }
                // 如果项目项是文件夹，则递归遍历其子项
                else if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                {
                    files.AddRange(TraverseProjectItems(projectItem.ProjectItems));
                }
            }
            return files;
        }

        private async void StatisticianButton_Click(object sender, RoutedEventArgs e)
        {
            if(ProjectComboBox.SelectedItem is not null)
            {

                var project = (Project)ProjectComboBox.SelectedItem;
                var counts = 0;
                ResultTextBlock.Text = "正在统计中...";
                RefreshExtension();
                var files = TraverseProjectItems(project.ProjectItems);
                Debug.WriteLine(files.Count);
                ResultTextBlock.Text = $"一共有 {files.Count} 个文件{Environment.NewLine}";
                foreach (var file in files)
                {
                    var filecount = GetFileCodeLineCount(file);
                    ResultTextBlock.Text += $"{file}：{filecount.ToString()}{Environment.NewLine}";
                    counts +=filecount;
                }
                MessageBox.Show(counts.ToString(),"结果",MessageBoxButton.OK,MessageBoxImage.Information);
                ResultTextBlock.Text += $"总计：{counts.ToString()}";
            }
            else
            {
                MessageBox.Show("请选择项目!");
            }
        }

        private void RefreshProjectButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshProjectList();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            extensionsInput.Text = ".cs;.cpp;.java;.js;.ts;.vb;.py;.rb;.go;.swift;.php;.html;.css;.xml";
            RefreshExtension();
        }
        private void RefreshExtension()
        {
            extensionsList = extensionsInput.Text.Split(';').ToList();
        }

        private int GetFileCodeLineCount(string path)
        {
            var counts = 0;
            var excludeEmptyLine = false;
            if(ExcludeEmptyLineCheckBox.IsChecked is not null)
            {
                excludeEmptyLine = ExcludeEmptyLineCheckBox.IsChecked.Value;
            }
            var excludeCommentLine = false;
            if (ExcludeCommentLineCheckBox.IsChecked is not null)
            {
                excludeCommentLine = ExcludeCommentLineCheckBox.IsChecked.Value;
            }
            if (File.Exists(path))
            {
                var lines = File.ReadLines(path).ToList();
                foreach (var line in lines)
                {
                    if (excludeEmptyLine)
                    {
                        if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line))
                        {
                            continue;
                        }
                    }
                    if (excludeCommentLine)
                    {
                        if (line.Trim().StartsWith("//") || line.Trim().StartsWith("#"))
                        {
                            continue;
                        }
                    }
                    counts++;
                }
            }
            Debug.WriteLine(path + " " + counts);
            return counts;
        }
    }
}