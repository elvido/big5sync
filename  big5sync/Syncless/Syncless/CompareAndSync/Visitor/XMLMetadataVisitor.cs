﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syncless.CompareAndSync.CompareObject;
using System.IO;
using System.Xml;
using Syncless.CompareAndSync.Enum;

namespace Syncless.CompareAndSync.Visitor
{
    public class XMLMetadataVisitor : IVisitor
    {
        private const string META_DIR = ".syncless";
        private const string XML_NAME = @"\syncless.xml";
        private const string METADATAPATH = META_DIR + @"\syncless.xml";
        private const string XPATH_EXPR = "/meta-data";
        private const string NODE_NAME = "name";
        private const string NODE_SIZE = "size";
        private const string NODE_HASH = "hash";
        private const string NODE_LAST_MODIFIED = "last_modified";
        private const string NODE_LAST_CREATED = "last_created";
        private const string FILES = "files";

        #region IVisitor Members

        public void Visit(FileCompareObject file, string[] currentPaths)
        {
            XmlDocument xmlDoc = new XmlDocument();
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (currentPaths[i].Contains(META_DIR))
                    continue;
                string path = Path.Combine(currentPaths[i], METADATAPATH);
                if (!File.Exists(path))
                    continue;
                xmlDoc.Load(path);
                file = PopulateFileWithMetaData(xmlDoc, file, i);                
                //xmlDoc.Save(path);                
            }
            xmlDoc = null;
            ProcessFileMetaData(file, currentPaths);
        }

        public void Visit(FolderCompareObject folder, string[] currentPaths)
        {
            XmlDocument xmlDoc = new XmlDocument();
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (currentPaths[i].Contains(META_DIR))
                    continue;
                string path = Path.Combine(currentPaths[i], METADATAPATH);
                if (!File.Exists(path))
                    continue;
                xmlDoc.Load(path);
                folder = PopulateFolderWithMetaData(xmlDoc, folder, i);                
                //xmlDoc.Save(path);               
            }
            ProcessFolderMetaData(folder, currentPaths);

            //EXPERIMENTAL
            
            DirectoryInfo dirInfo = null;
            FileInfo [] fileList = null;
            DirectoryInfo [] dirList = null;
            List<XMLCompareObject> xmlObjList = new List<XMLCompareObject>();
            string xmlPath = "";

            for(int i = 0 ; i< currentPaths.Length;i++)
            {
                string path = Path.Combine(currentPaths[i],folder.Name);
                

                if (Directory.Exists(path))
                {
                    dirInfo = new DirectoryInfo(path);
                    fileList = dirInfo.GetFiles();
                    dirList = dirInfo.GetDirectories();
                    xmlPath = Path.Combine(path,METADATAPATH);
                    if (!File.Exists(xmlPath))
                        continue;
                    xmlDoc.Load(xmlPath);
                    xmlObjList = GetAllFilesInXML(xmlDoc);
                    RemoveSimilarFiles(xmlObjList, fileList);
                }

                if (xmlObjList.Count == 0)
                    continue;

                AddToChild(xmlObjList, folder, i , currentPaths.Length);
                xmlObjList = new List<XMLCompareObject>();
            }



        }

        public void Visit(RootCompareObject root)
        {
            //
        }

        #endregion

        #region Files

        private FileCompareObject PopulateFileWithMetaData(XmlDocument xmlDoc, FileCompareObject file, int counter)
        {
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/files" + "[name='" + file.Name + "']");
            if (node == null)
                return file;

            XmlNodeList childNodeList = node.ChildNodes;
            for (int i = 0; i < childNodeList.Count; i++)
            {
                XmlNode childNode = childNodeList[i];
                if (childNode.Name.Equals(NODE_SIZE))
                {
                    file.MetaLength[counter] = long.Parse(childNode.InnerText);
                }
                else if (childNode.Name.Equals(NODE_HASH))
                {
                    file.MetaHash[counter] = childNode.InnerText;
                }
                else if (childNode.Name.Equals(NODE_LAST_MODIFIED))
                {
                    file.MetaLastWriteTime[counter] = long.Parse(childNode.InnerText);
                }
                else if (childNode.Name.Equals(NODE_LAST_CREATED))
                {
                    file.MetaCreationTime[counter] = long.Parse(childNode.InnerText);
                }
            }

            file.MetaExists[counter] = true;
            return file;

        }

        private List<XMLCompareObject> GetAllFilesInXML(XmlDocument xmlDoc)
        {
            string hash = "";
            string name = "";
            long size = 0;
            long createdTime = 0;
            long modifiedTime = 0;

            List<XMLCompareObject> objectList = new List<XMLCompareObject>();
            XmlNodeList xmlNodeList = xmlDoc.SelectNodes(XPATH_EXPR + "/files");
            if (xmlNodeList == null)
                return objectList;

            foreach (XmlNode nodes in xmlNodeList)
            {
                XmlNodeList list = nodes.ChildNodes;
                foreach (XmlNode node in list)
                {
                    switch (node.Name)
                    {
                        case NODE_NAME:
                            name = node.InnerText;
                            break;
                        case NODE_SIZE:
                            size = long.Parse(node.InnerText);
                            break;
                        case NODE_HASH:
                            hash = node.InnerText;
                            break;
                        case NODE_LAST_CREATED:
                            createdTime = long.Parse(node.InnerText);
                            break;
                        case NODE_LAST_MODIFIED:
                            modifiedTime = long.Parse(node.InnerText);
                            break;
                    }
                }
                objectList.Add(new XMLCompareObject(name, hash, size, createdTime, modifiedTime));
            }

            return objectList;
        }

        private void RemoveSimilarFiles(List<XMLCompareObject> xmlObjList, FileInfo[] fileList)
        {
            if (xmlObjList.Count == 0)
                return ;

            for (int i = 0; i < fileList.Length; i++)
            {
                for (int j = 0; j < xmlObjList.Count; j++)
                {
                    FileInfo fileInfo = fileList[i];
                    string name = xmlObjList[j].Name;
                    if (name.Equals(fileInfo.Name))
                        xmlObjList.RemoveAt(j);
                }
            }
        }

        private void AddToChild(List<XMLCompareObject> xmlFileList, FolderCompareObject folder, int counter, int length)
        {
            for (int i = 0; i < xmlFileList.Count; i++)
            {
                BaseCompareObject o = folder.GetChild(xmlFileList[i].Name);
                FileCompareObject fco = null;

                if (o == null)
                    fco = new FileCompareObject(xmlFileList[i].Name, length , folder);
                else
                    fco = (FileCompareObject)o;

                fco.MetaCreationTime[counter] = xmlFileList[i].CreatedTime;
                fco.MetaHash[counter] = xmlFileList[i].Hash;
                fco.MetaLastWriteTime[counter] = xmlFileList[i].LastModifiedTime;
                fco.MetaLength[counter] = xmlFileList[i].Size;
                fco.MetaExists[counter] = true;

                if (o == null)
                    folder.AddChild(fco);
            }
        }

        private void ProcessFileMetaData(FileCompareObject file, string[] currentPaths)
        {
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (file.Exists[i] && !file.MetaExists[i])
                    file.ChangeType[i] = MetaChangeType.New; //Possible rename/move
                else if (!file.Exists[i] && file.MetaExists[i])
                    file.ChangeType[i] = MetaChangeType.Delete; //Possible rename/move
                else if (file.Exists[i] && file.MetaExists[i])
                {
                    if (file.Length[i] != file.MetaLength[i] || file.Hash[i] != file.MetaHash[i])
                        file.ChangeType[i] = MetaChangeType.Update;
                    else
                        file.ChangeType[i] = MetaChangeType.NoChange;
                }
                else
                    file.ChangeType[i] = null;
            }
        }

        #endregion

        #region Folders

        private FolderCompareObject PopulateFolderWithMetaData(XmlDocument xmlDoc, FolderCompareObject folder, int counter)
        {
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/folder" + "[name='" + folder.Name + "']");
            if (node == null)
                return folder;

            folder.MetaExists[counter] = true;
            return folder;
        }

        private void ProcessFolderMetaData(FolderCompareObject folder, string[] currentPaths)
        {
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (folder.Exists[i] && !folder.MetaExists[i])
                    folder.ChangeType[i] = MetaChangeType.New; //Possible rename/move
                else if (!folder.Exists[i] && folder.MetaExists[i])
                    folder.ChangeType[i] = MetaChangeType.Delete; //Possible rename/move
                else if (folder.Exists[i] && folder.MetaExists[i])
                    folder.ChangeType[i] = MetaChangeType.NoChange;
                else
                    folder.ChangeType[i] = null;
            }
        }

        #endregion

    }
}
