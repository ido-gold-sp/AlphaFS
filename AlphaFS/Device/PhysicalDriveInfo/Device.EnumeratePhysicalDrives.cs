/*  Copyright (C) 2008-2017 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Device
   {
      /// <summary>[AlphaFS] Enumerates the physical drives on the Computer, populated with volume- and logical drive information.</summary>
      /// <returns>An <see cref="IEnumerable{PhysicalDriveInfo}"/> collection that represents the physical drives on the Computer.</returns>      
      [SecurityCritical]
      public static IEnumerable<PhysicalDriveInfo> EnumeratePhysicalDrives()
      {
         return EnumeratePhysicalDrivesCore(Security.ProcessContext.IsElevatedProcess);
      }


      
      
      /// <summary>[AlphaFS] Enumerates the physical drives on the Computer, populated with volume- and logical drive information.</summary>
      /// <returns>An <see cref="IEnumerable{PhysicalDriveInfo}"/> collection that represents the physical drives on the Computer.</returns>      
      [SecurityCritical]
      internal static IEnumerable<PhysicalDriveInfo> EnumeratePhysicalDrivesCore(bool isElevated)
      {
         var physicalDrives = EnumerateDevicesCore(null, DeviceGuid.Disk, false).Select(deviceInfo => GetPhysicalDriveInfoCore(isElevated, null, null, deviceInfo)).Where(physicalDrive => null != physicalDrive).ToArray();

         var pVolumeGuids = Volume.EnumerateVolumes().Select(volumeGuid => GetPhysicalDriveInfoCore(false, volumeGuid, null, null)).Where(physicalDrive => null != physicalDrive).ToArray();

         var pLogicalDrives = DriveInfo.EnumerateLogicalDrivesCore(false, false).Select(driveName => GetPhysicalDriveInfoCore(false, driveName, null, null)).Where(physicalDrive => null != physicalDrive).ToArray();


         foreach (var pDrive in physicalDrives)

            yield return PopulatePhysicalDrive(pDrive, pVolumeGuids, pLogicalDrives);


         // Windows Disk Management shows CD-ROM so mimic that behaviour.

         var cdRoms = EnumerateDevicesCore(null, DeviceGuid.CDRom, false).Select(deviceInfo => GetPhysicalDriveInfoCore(isElevated, null, null, deviceInfo)).Where(physicalDrive => null != physicalDrive).ToArray();

         foreach (var pCdRom in cdRoms)

            yield return PopulatePhysicalCDRom(pCdRom, pVolumeGuids, pLogicalDrives);
      }


      private static void PopulateLogicalDriveDetails(PhysicalDriveInfo pDriveInfo, string drivePath)
      {
         // Add device logical drive.

         if (null == pDriveInfo.LogicalDrives)
            pDriveInfo.LogicalDrives = new Collection<string>();

         pDriveInfo.LogicalDrives.Add(Path.RemoveTrailingDirectorySeparator(drivePath));
      }


      private static void PopulateVolumeDetails(PhysicalDriveInfo pDriveInfo, int partitionNumber, string volumeGuid)
      {
         //// Add device volume labels.

         //if (null == pDriveInfo.VolumeLabels)
         //   pDriveInfo.VolumeLabels = new Collection<string>();

         //pDriveInfo.VolumeLabels.Add(pVolume.Name);


         // Add device partition index numbers.

         if (null == pDriveInfo.PartitionIndexes)
            pDriveInfo.PartitionIndexes = new Collection<int>();

         pDriveInfo.PartitionIndexes.Add(partitionNumber);


         // Add device volume GUIDs.

         if (null == pDriveInfo.VolumeGuids)
            pDriveInfo.VolumeGuids = new Collection<string>();

         pDriveInfo.VolumeGuids.Add(volumeGuid);
      }


      private static PhysicalDriveInfo PopulatePhysicalCDRom(PhysicalDriveInfo pCdRom, PhysicalDriveInfo[] pVolumes, PhysicalDriveInfo[] pLogicalDrives)
      {
         var pDriveInfo = new PhysicalDriveInfo(pCdRom) {StorageDeviceInfo = pCdRom.StorageDeviceInfo};


         // Get volume from CDRom matching DeviceNumber.

         var pVolume = pVolumes.SingleOrDefault(pVol => pVol.StorageDeviceInfo.DeviceNumber == pDriveInfo.StorageDeviceInfo.DeviceNumber && pVol.StorageDeviceInfo.PartitionNumber == pDriveInfo.StorageDeviceInfo.PartitionNumber);

         if (null != pVolume)
         {
            PopulateVolumeDetails(pDriveInfo, pVolume.StorageDeviceInfo.PartitionNumber, pVolume.DevicePath);


            // Get logical drive from CDRom matching DeviceNumber and PartitionNumber.

            var pLogicalDrive = pLogicalDrives.SingleOrDefault(pDriveLogical => pDriveLogical.StorageDeviceInfo.DeviceNumber == pVolume.StorageDeviceInfo.DeviceNumber && pDriveLogical.StorageDeviceInfo.PartitionNumber == pVolume.StorageDeviceInfo.PartitionNumber);

            if (null != pLogicalDrive)
               PopulateLogicalDriveDetails(pDriveInfo, pLogicalDrive.DevicePath);
         }


         return pDriveInfo;
      }


      private static PhysicalDriveInfo PopulatePhysicalDrive(PhysicalDriveInfo pDrive, PhysicalDriveInfo[] pVolumes, PhysicalDriveInfo[] pLogicalDrives)
      {
         var pDriveInfo = new PhysicalDriveInfo(pDrive) {StorageDeviceInfo = pDrive.StorageDeviceInfo};


         foreach (var pVolume in pVolumes.Where(pVol => pVol.StorageDeviceInfo.DeviceNumber == pDriveInfo.StorageDeviceInfo.DeviceNumber))
         {
            var volumeDriveNumber = pVolume.StorageDeviceInfo.DeviceNumber;
            var volumePartitionNumber = pVolume.StorageDeviceInfo.PartitionNumber;


            PopulateVolumeDetails(pDriveInfo, pVolume.StorageDeviceInfo.PartitionNumber, pVolume.DevicePath);


            // Get logical drive from volume matching DeviceNumber and PartitionNumber.

            foreach (var pLogicalDrive in pLogicalDrives.Where(pDriveLogical => pDriveLogical.StorageDeviceInfo.DeviceNumber == volumeDriveNumber && pDriveLogical.StorageDeviceInfo.PartitionNumber == volumePartitionNumber))

               PopulateLogicalDriveDetails(pDriveInfo, pLogicalDrive.DevicePath);
         }


         return pDriveInfo;
      }
   }
}