#!/usr/bin/env python
import glob, os, sys 
import itk
import numpy as np
import re
import utils.calc_ROI as cr


def Boundingboxed(data_path, label_path, output_dir, bb_dir, number, phase):

    #annotations = ['Gastroduodenalis', 'AMS', 'Aorta', 'Pancreas', 'Splenic vein', 'Truncus', 'Vena Cava', 'Vena porta', 'VMI', 'Tumour']
    annotations = ['Tumour', 'Pancreas']

    bb_dir = os.path.join(bb_dir, number, phase)
    bb_file = os.path.join(bb_dir, "*.mrml")
    
    for file in glob.glob(bb_file):
        bb_file = file

    if not os.path.exists(bb_file):
        return

    if not os.path.exists(label_path):
        return

    PixelType = itk.ctype('signed short')
    Dimension = 3
    ImageType = itk.Image[PixelType, Dimension]
    # get bounding box for series
    min_coord, max_coord = cr.calc_ROI(bb_file)

    print(min_coord)

    print(max_coord)

    if(min_coord[2].item() <= 0 ):
        min_coord[2] = np.array([0])

    start = itk.Index[Dimension]()
    start[0] = min_coord[0].item() # startx
    start[1] = min_coord[1].item() # starty
    start[2] = min_coord[2].item() # start along Z

    end = itk.Index[Dimension]()
    end[0] = max_coord[0].item() # endx
    end[1] = max_coord[1].item() # endy
    end[2] = max_coord[2].item() # size along Z

    # roi_start = itk.Index[2]()
    # roi_start[0] = min_coord[0].item() # startx
    # roi_start[1] = min_coord[1].item() # starty

    # roi_end = itk.Index[2]()
    # roi_end[0] = max_coord[0].item() # endx
    # roi_end[1] = max_coord[1].item() # endy

    region = itk.ImageRegion[Dimension]()
    region.SetIndex(start)
    region.SetUpperIndex(end)

    # region2D = itk.ImageRegion[2]()
    # region2D.SetIndex(roi_start)
    # region2D.SetUpperIndex(roi_end)

    output = os.path.join(output_dir,  number, phase)
    output_label = os.path.join(output, 'label')
    output_image = os.path.join(output, 'image')

    if not os.path.exists(output_label):
        os.makedirs(output_label)
    if not os.path.exists(output_image):
        os.makedirs(output_image)

    namesGenerator = itk.GDCMSeriesFileNames.New()
    namesGenerator.SetUseSeriesDetails(True)
    namesGenerator.AddSeriesRestriction("0008|0021")
    namesGenerator.SetGlobalWarningDisplay(False)
    namesGenerator.SetDirectory(data_path)

    seriesUID = namesGenerator.GetSeriesUIDs()
    series_name = phase + '_' + number

    if len(seriesUID) < 1:
        print('No DICOMs in: ' + data_path)
        return

    seriesFound = False
    for uid in seriesUID:
        seriesIdentifier = uid

        print('Reading: ' + seriesIdentifier)

        fileNames = namesGenerator.GetFileNames(seriesIdentifier)

        reader = itk.ImageSeriesReader[ImageType].New()
        dicomIO = itk.GDCMImageIO.New()
        reader.SetImageIO(dicomIO)
        reader.SetFileNames(fileNames)
        reader.ForceOrthogonalDirectionOff()
        #reader.Update()


        # reader_header = dicomIO.GetMetaDataDictionary()
        # dicom_header = itk.MetaDataObject()

        # start_dict = dictionary.Begin()
        # end_dict = dictionary.End()

        size = itk.size(reader.GetOutput())
        print("reader")
        print(size)
        print(reader.GetOutput())

        if size[2] <=2:
            break
        # Apply region of interest
        # ROI = itk.ExtractImageFilter[ImageType, ImageType].New()
        # ROI.SetExtractionRegion(region)
        # ROI.SetInput(reader.GetOutput())

        ROI = itk.RegionOfInterestImageFilter[ImageType, ImageType].New()
        ROI.SetInput(reader.GetOutput())
        ROI.SetRegionOfInterest(region)
        #ROI.SetMetaDataDictionary(reader.GetMetaDataDictionary())
        ROI.Update()

        print("ROI")
        size = itk.size(ROI)
        print(size)
        print(ROI.GetOutput())



        all_labels = []

        for ann in annotations:

            print('ann:', ann)

            labelfilelist = [file for file in os.listdir(label_path) if ann.lower() in file.lower()]

            if len(labelfilelist) > 0:

                labelfile = labelfilelist[0]

                print('labelfile: ', labelfile)
                labelpath = os.path.join(label_path, labelfile)

                
                MeshType = itk.Mesh[itk.SS,3]
                
                meshReader = itk.MeshFileReader[MeshType].New()
                meshReader.SetFileName(labelpath)
                meshReader.Update()

                #print('Meshreaderout: ', meshReader.GetOutput())

                #ImageType = itk.Image[itk.F, 3]

                filter = itk.TriangleMeshToBinaryImageFilter[MeshType, ImageType].New()
                filter.SetInput(meshReader.GetOutput())
                filter.SetInfoImage(reader.GetOutput())
                filter.Update()

                # size = itk.size(filter)
                # print("filter")
                # print(size)
                # print(filter.GetOutput())

                MeshROI = itk.ExtractImageFilter[ImageType, ImageType].New()
                MeshROI.SetExtractionRegion(region)
                MeshROI.SetInput(filter.GetOutput())
                MeshROI.Update()
        
                size = itk.size(MeshROI)
                print("MeshROI")
                print(size)
                #print(MeshROI.GetOutput())

                #print('filterout: ', filter.GetOutput())
                image = np.array(itk.array_from_image(MeshROI.GetOutput())).astype(np.bool)
                all_labels.append(image)

            else:
                
                imsize = ROI.GetOutput().GetLargestPossibleRegion().GetSize()
                print(f"Failed to find {ann} ")
                image = np.zeros((imsize[2], imsize[1], imsize[0])).astype(np.bool)

                all_labels.append(image)

        # process label
        print([ls.shape for ls in all_labels])

        labels = np.stack(all_labels)
        labelmap = labels.astype(np.uint8)
        roi_size = itk.size(ROI)
        
        new_labelmap = np.concatenate((np.zeros((1,roi_size[2],roi_size[1],roi_size[0])).astype(np.uint8), labelmap), axis=0)
        
        labelmap = new_labelmap.argmax(axis=0)
        labelmap = labelmap.astype(np.short)
        labelmap[-1, :,:] = 0.

        labelType = ImageType
        labelmap = np.ascontiguousarray(labelmap)
        itk_image = itk.GetImageFromArray(labelmap)
        itk_image.SetMetaDataDictionary(ROI.GetMetaDataDictionary())
        itk_image.Update()

        header = itk.ChangeInformationImageFilter[ImageType].New()
        header.SetInput(itk_image)
        #print("Header filter")
        header.SetOutputSpacing(ROI.GetOutput().GetSpacing())
        header.ChangeSpacingOn()
        header.SetOutputOrigin(ROI.GetOutput().GetOrigin())
        header.ChangeOriginOn()
        header.SetOutputDirection(ROI.GetOutput().GetDirection())
        header.ChangeDirectionOn()
        header.UpdateOutputInformation()
        header.Update()
        #header.ChangeNone()
        #print(header.GetOutput())

        writer = itk.ImageFileWriter[labelType].New()
        writer.SetInput(header.GetOutput())
        segmentation_name = os.path.join(output_label, series_name + '.nii.gz')
        print(segmentation_name)
        writer.SetFileName(segmentation_name)
        writer.Update()


        metadata = ROI.GetMetaDataDictionary()
        print('metadata: ', metadata)

        data_writer = itk.ImageFileWriter[ImageType].New()
        outFileName = os.path.join(output_image, series_name + '_0000.nii.gz')
        data_writer.SetFileName(outFileName)
        data_writer.UseCompressionOn()
        #data_writer.UseInputMetaDataDictionaryOn ()
        data_writer.SetInput(ROI.GetOutput())
        

        print('Writing: ' + outFileName)
        
        data_writer.Update()

        if seriesFound:
            break

def full3D_all_labels(data_path, label_path, output_dir, number, phase):

    annotations = ['Gastroduodenalis', 'AMS', 'Aorta', 'Pancreas', 'Splenic vein', 'Truncus', 'Vena Cava', 'Vena porta', 'VMI', 'Tumour']
    # annotations = ['Gastroduodenalis', 'Splenic vein', 'VMI', 'Vena Cava', 'Aorta',  'Truncus', "AMS", 'Vena porta', 'Pancreas',  'Tumour']
    # annotations = ['Gastroduodenalis', 'Splenic vein', 'VMI', 'Vena Cava', 'Aorta',  'Truncus', "AMS", 'Pancreas',  'Vena porta','Tumour'] # Vessel involvement
    # annotations = ['Pancreas', 'Tumour']
    # annotations = []

    if not os.path.exists(label_path):
        return

    PixelType = itk.ctype('signed short')
    Dimension = 3
    ImageType = itk.Image[PixelType, Dimension]


    output = os.path.join(output_dir,  number, phase)
    output_label = os.path.join(output, 'labelsTr')
    output_image = os.path.join(output, 'imagesTr')

    if not os.path.exists(output_label):
        os.makedirs(output_label)
    if not os.path.exists(output_image):
        os.makedirs(output_image)

    namesGenerator = itk.GDCMSeriesFileNames.New()
    namesGenerator.SetUseSeriesDetails(True)
    namesGenerator.AddSeriesRestriction("0008|0021")
    namesGenerator.SetGlobalWarningDisplay(False)
    namesGenerator.SetDirectory(data_path)

    seriesUID = namesGenerator.GetSeriesUIDs()
    series_name = phase + '_' + number

    if len(seriesUID) < 1:
        print('No DICOMs in: ' + data_path)
        return

    seriesFound = False
    for uid in seriesUID:
        seriesIdentifier = uid

        print('Reading: ' + seriesIdentifier)

        fileNames = namesGenerator.GetFileNames(seriesIdentifier)

        reader = itk.ImageSeriesReader[ImageType].New()
        dicomIO = itk.GDCMImageIO.New()
        reader.SetImageIO(dicomIO)
        reader.SetFileNames(fileNames)
        reader.ForceOrthogonalDirectionOff()
        #reader.Update()


        size = itk.size(reader.GetOutput())
        print("reader")
        print(size)
        print(reader.GetOutput())

        if size[2] <=2:
            break

        all_labels = []
        if len(annotations) != 0:
        
            for count, ann in enumerate( annotations):

                print('ann:', ann)

                labelfilelist = [file for file in os.listdir(label_path) if ann.lower() in file.lower()]

                if len(labelfilelist) > 0:

                    labelfile = labelfilelist[0]

                    print('labelfile: ', labelfile)
                    labelpath = os.path.join(label_path, labelfile)

                    
                    MeshType = itk.Mesh[itk.SS,3]
                    
                    meshReader = itk.MeshFileReader[MeshType].New()
                    meshReader.SetFileName(labelpath)
                    meshReader.Update()

                    #print('Meshreaderout: ', meshReader.GetOutput())

                    #ImageType = itk.Image[itk.F, 3]

                    filter = itk.TriangleMeshToBinaryImageFilter[MeshType, ImageType].New()
                    filter.SetInput(meshReader.GetOutput())
                    filter.SetInfoImage(reader.GetOutput())
                    filter.Update()


                    # print('filterout: ', filter.GetOutput())

                    image = np.array(itk.array_from_image(filter.GetOutput())).astype(np.bool) * (count+1)
                    all_labels.append(image)

                else:
                    
                    imsize = reader.GetOutput().GetLargestPossibleRegion().GetSize()
                    print(f"Failed to find {ann} ")
                    image = np.zeros((imsize[2], imsize[1], imsize[0])).astype(np.bool)

                    all_labels.append(image)
        

        # process label
        print([ls.shape for ls in all_labels])

        labels = np.stack(all_labels)
        all_labels = None
        labelmap = labels.astype(np.uint8)

        reader_size = itk.size(reader) ## changed this from filter
        
        new_labelmap = np.concatenate((np.zeros((1,reader_size[2],reader_size[1],reader_size[0])).astype(np.uint8), labelmap), axis=0)
        #new_labelmap = np.zeros((1,reader_size[2],reader_size[1],reader_size[0])).astype(np.uint8)
        labelmap = new_labelmap.argmax(axis=0)
        labelmap = labelmap.astype(np.short)
        

        labelType = ImageType
        labelmap = np.ascontiguousarray(labelmap)
        itk_image = itk.GetImageFromArray(labelmap)
        itk_image.SetMetaDataDictionary(reader.GetMetaDataDictionary())
        itk_image.Update()

        header = itk.ChangeInformationImageFilter[ImageType].New()
        header.SetInput(itk_image)
        #print("Header filter")
        header.SetOutputSpacing(reader.GetOutput().GetSpacing())
        header.ChangeSpacingOn()
        header.SetOutputOrigin(reader.GetOutput().GetOrigin())
        header.ChangeOriginOn()
        header.SetOutputDirection(reader.GetOutput().GetDirection())
        header.ChangeDirectionOn()
        header.UpdateOutputInformation()
        #header.Update()

        writer = itk.ImageFileWriter[labelType].New()
        writer.SetInput(header.GetOutput())
        segmentation_name = os.path.join(output_label, series_name + '.nii.gz')
        print(segmentation_name)
        writer.SetFileName(segmentation_name)
        writer.Update()


        # metadata = reader.GetMetaDataDictionary()
        # print('metadata: ', metadata)

        data_writer = itk.ImageFileWriter[ImageType].New()
        outFileName = os.path.join(output_image, series_name + '_0000.nii.gz')
        data_writer.SetFileName(outFileName)
        data_writer.UseCompressionOn()
        data_writer.UseInputMetaDataDictionaryOn ()
        data_writer.SetInput(reader.GetOutput())
        

        print('Writing: ' + outFileName)
        
        data_writer.Update()

        if seriesFound:
            break 


def seperate_labels(data_path, label_path, output_dir, number, phase):

    annotations = ['Gastroduodenalis', 'AMS', 'Aorta', 'Pancreas', 'Splenic vein', 'Truncus', 'Vena Cava', 'Vena porta', 'VMI', 'Tumour',  'Abb', 'Hep']

    if not os.path.exists(label_path):
        return

    PixelType = itk.ctype('signed short')
    Dimension = 3
    ImageType = itk.Image[PixelType, Dimension]


    output = os.path.join(output_dir,  number, phase)
    output_label = os.path.join(output, 'labelsTr')
    output_image = os.path.join(output, 'imagesTr')

    if not os.path.exists(output_label):
        os.makedirs(output_label)
    if not os.path.exists(output_image):
        os.makedirs(output_image)

    namesGenerator = itk.GDCMSeriesFileNames.New()
    namesGenerator.SetUseSeriesDetails(True)
    namesGenerator.AddSeriesRestriction("0008|0021")
    namesGenerator.SetGlobalWarningDisplay(False)
    namesGenerator.SetDirectory(data_path)

    seriesUID = namesGenerator.GetSeriesUIDs()
    series_name = phase + '_' + number

    if len(seriesUID) < 1:
        print('No DICOMs in: ' + data_path)
        return

    seriesFound = False
    for uid in seriesUID:
        seriesIdentifier = uid

        print('Reading: ' + seriesIdentifier)

        fileNames = namesGenerator.GetFileNames(seriesIdentifier)

        reader = itk.ImageSeriesReader[ImageType].New()
        dicomIO = itk.GDCMImageIO.New()
        reader.SetImageIO(dicomIO)
        reader.SetFileNames(fileNames)
        reader.ForceOrthogonalDirectionOff()
        #reader.Update()

        size = itk.size(reader.GetOutput())
        print("reader")
        print(size)
        print(reader.GetOutput())

        original_labeled_files = os.listdir(label_path) 
        print(original_labeled_files)

        if size[2] <=2:
            break

        if len(annotations) != 0:
        
            for count, ann in enumerate( annotations):

                print('ann:', ann)

                labelfilelist = [file for file in original_labeled_files if ann.lower() in file.lower()] # definily not the most efficient way to do this
                original_labeled_files = [x for x in original_labeled_files if x not in labelfilelist] # ensure an annotation is only used once

                if len(labelfilelist) == 1:

                    labelfile = labelfilelist[0]

                    print('labelfile: ', labelfile)
                    labelpath = os.path.join(label_path, labelfile)

                    
                    MeshType = itk.Mesh[itk.SS,3]
                    meshReader = itk.MeshFileReader[MeshType].New()
                    meshReader.SetFileName(labelpath)
                    meshReader.Update()


                    filter = itk.TriangleMeshToBinaryImageFilter[MeshType, ImageType].New()
                    filter.SetInput(meshReader.GetOutput())
                    filter.SetInfoImage(reader.GetOutput())
                    filter.Update()

                    image = np.array(itk.array_from_image(filter.GetOutput())).astype(np.short) 
                    labelmap = np.ascontiguousarray(image)

                    itk_image = itk.GetImageFromArray(labelmap)
                    itk_image.SetMetaDataDictionary(reader.GetMetaDataDictionary())
                    itk_image.Update()

                    header = itk.ChangeInformationImageFilter[ImageType].New()
                    header.SetInput(itk_image)
                    header.SetOutputSpacing(reader.GetOutput().GetSpacing())
                    header.ChangeSpacingOn()
                    header.SetOutputOrigin(reader.GetOutput().GetOrigin())
                    header.ChangeOriginOn()
                    header.SetOutputDirection(reader.GetOutput().GetDirection())
                    header.ChangeDirectionOn()
                    header.UpdateOutputInformation()
                    #header.Update()

                    writer = itk.ImageFileWriter[ImageType].New()
                    writer.SetInput(header.GetOutput())
                    segmentation_name = os.path.join(output_label, series_name + '_' +ann+ '.nii.gz')
                    print(segmentation_name)
                    writer.SetFileName(segmentation_name)
                    writer.Update()

                elif len(labelfilelist) > 1:
                    print(f"Multiple labels for {ann} ")

                else:
                    print(f"Failed to find {ann} ")

        metadata = reader.GetMetaDataDictionary()
        print('metadata: ', metadata)

        data_writer = itk.ImageFileWriter[ImageType].New()
        outFileName = os.path.join(output_image, series_name + '.nii.gz')
        data_writer.SetFileName(outFileName)
        data_writer.UseCompressionOn()
        data_writer.UseInputMetaDataDictionaryOn ()
        data_writer.SetInput(reader.GetOutput())

        print('Writing: ' + outFileName)
        data_writer.Update()

        if seriesFound:
            break 

if __name__ == "__main__":
    pass