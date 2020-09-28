using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Apololab.Common.Http.Exception;
using Apololab.Common.Http.Rest;
using DrsBee.API;

namespace DrsBeePharmacyApiDemo
{
    class MainClass
    {

        static DrsBeeConfig CONFIG = DrsBeeConfig.DEV_CR;
        const string API_KEY_RESOURCE = "apiClientPharmaGroupClerk1.key"; // El archivo esta incluído en el proyecto como resource, en el directorio "Resources"
        const string API_KEY_ACCOUNT = "pharmagroupclerk1@test.com";

        static UserServices userServices = new UserServices(CONFIG);
        static PharmacyServices pharmacyServices = new PharmacyServices(CONFIG);
        static CatalogServices catalogServices = new CatalogServices(CONFIG);

        public static void Main(string[] args)
        {
            try
            {
                // Inicializamos el token para los servicios de API
                using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(API_KEY_RESOURCE))
                {
                    DrsBeeAuth.InitApi(API_KEY_ACCOUNT, resourceStream);
                }

                LoginSuccess login = userServices.LoginAsHealthProfessionalByCert().Result;
                if (login == null)
                {
                    throw new Exception("No se pudo realizar login");
                }

                ApiPharmacyPrescriptionsToDeliver prescriptionsToDeliver = pharmacyServices.GetPrescriptionsToDeliverAsync().Result;

                //Para validar el tipo de identificación
                List<IdentificationType> identificationTypes = catalogServices.GetIdentificationTypesAsync().Result;

                foreach (ApiPharmacyPrescription apiPharmacyPrescription in prescriptionsToDeliver.prescriptionsToDeliver)
                {
                    if (apiPharmacyPrescription.quote != null && apiPharmacyPrescription.externalPharmacyCode != null && apiPharmacyPrescription.quote.express &&
                         apiPharmacyPrescription.quote.addressInfo != null && apiPharmacyPrescription.quote.addressInfo.politicalRegion != null && apiPharmacyPrescription.quote.addressInfo != null && apiPharmacyPrescription.quote.billingInfo != null)
                    {

                        //Encabezado
                        Console.WriteLine(
                            "Código de farmacia: " + apiPharmacyPrescription.externalPharmacyCode + "\n" +
                            "Nombre del cliente: " + apiPharmacyPrescription.patient.firstName + " " + apiPharmacyPrescription.patient.lastName + "\n" +
                            //*Facturación
                            "Tipo de identificación: " + apiPharmacyPrescription.quote.billingInfo.identificationTypeCode + "\n" +
                            "Número de identificación: " + apiPharmacyPrescription.quote.billingInfo.identification + "\n" +
                            "Email_Facturación: " + apiPharmacyPrescription.quote.billingInfo.email + "\n" +
                            "Nombre para Facturación: " + apiPharmacyPrescription.quote.billingInfo.name + "\n" +
                            //**
                            "Fecha_Documento: " + apiPharmacyPrescription.quote.quoteDate + "\n" +
                            "Telefono: " + apiPharmacyPrescription.patient.phoneNumber + "\n" +
                            "Email: " + apiPharmacyPrescription.patient.email + "\n" +
                            "Num_Aprobacion_Pago: " + apiPharmacyPrescription.quote.paymentTransaction.externalReference + "\n" +
                            "Fecha de pago: " + apiPharmacyPrescription.quote.paymentTransaction.date.ToString() + "\n" +
                            "Pago_Total: " + apiPharmacyPrescription.quote.totalPrice + "\n" +
                            "Monto_Total: " + apiPharmacyPrescription.quote.totalPrice + "\n");


                        //Region politica
                        if (apiPharmacyPrescription.quote.addressInfo != null)
                        {
                            PoliticalRegion politicalRegion = apiPharmacyPrescription.quote.addressInfo.politicalRegion;
                            if (politicalRegion != null && politicalRegion.parent != null && politicalRegion.parent.parent != null)
                            {
                                //Para comprobar esta información, puede ver politicalRegion.description
                                //El tipo de dato se distingue por su nivel: Level 1: Provincia, Level 2:Cantón, Level 3: Distrito  
                                Console.WriteLine(
                                    "Provincia_ID: " + politicalRegion.parent.parent.description + ": " + politicalRegion.parent.parent.number + "\n" +
                                    "Canton_ID: " + politicalRegion.parent.description + ": " + politicalRegion.parent.number + "\n" +
                                    "Distrito_ID: " + politicalRegion.description + ": " + politicalRegion.number);
                            }

                            //Señas dirección
                            Console.WriteLine(
                                  "Detalle de la dirección: " + apiPharmacyPrescription.quote.addressInfo.addressDetail + "\n" +
                                  "Lugar de entrega: " + apiPharmacyPrescription.quote.addressInfo.deliveryPlaceDetail);

                        }

                        //Detalle
                        Console.WriteLine(
                                "ID_Documento: " + apiPharmacyPrescription.id);


                        foreach (Dictionary<string, List<DrugPharmacyQuotePresentation>> keyValues in apiPharmacyPrescription.quote.quotePresentationsByDrugIdByVademecumId.Values)
                        {
                            foreach (List<DrugPharmacyQuotePresentation> drugPharmacyQuotePresentations in keyValues.Values)
                            {
                                foreach (DrugPharmacyQuotePresentation drugPharmacyQuotePresentation in drugPharmacyQuotePresentations)
                                {
                                    Console.WriteLine(
                                        "Codigo_Articulo: " + drugPharmacyQuotePresentation.pharmacyPresentationCode + "\n" +
                                        "Unidad: " + drugPharmacyQuotePresentation.loose + "\n" +
                                        "Cantidad: " + drugPharmacyQuotePresentation.quantity + "\n" +
                                        "Precio_Bruto_Unitario_DrsBee: " + drugPharmacyQuotePresentation.grossPrice + "\n" +
                                        "Precio_Bruto_Unitario indicado por farmacia: " + drugPharmacyQuotePresentation.unitaryPrice + "\n" +
                                        "Descuento_Porcentaje: " + drugPharmacyQuotePresentation.discountPercentage + "\n" +
                                        "Fecha vencimiento del descuento: " + (drugPharmacyQuotePresentation.discountDueDate != null ? drugPharmacyQuotePresentation.discountDueDate.ToString() : "-") + "\n" +
                                        "Impuesto_Porcentaje: " + drugPharmacyQuotePresentation.taxPercentage + "\n" +
                                        "Total_Linea: " + drugPharmacyQuotePresentation.totalPrice + "\n");
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Actualmente hay: " + prescriptionsToDeliver.prescriptionsToDeliver.Count + " prescription a entregar");
                ApiPharmacyPrescription prescriptionToDispense = prescriptionsToDeliver.prescriptionsToDeliver[prescriptionsToDeliver.prescriptionsToDeliver.Count - 1];
                Console.WriteLine("Dispensando primer prescripcion con id " + prescriptionToDispense.id + " pendiente de dispensar desde " + prescriptionToDispense.quote.lastModifiedDate);
                pharmacyServices.DispenseQuotedPrescriptionAsync(prescriptionToDispense.id).Wait();
                Console.WriteLine("Prescription dispensada");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                WebServiceException wsex = ex.InnerException != null ? ex.InnerException as WebServiceException : null;
                HttpException hex = ex.InnerException != null ? ex.InnerException as HttpException : null;
                // Error en drsbee backend con mensaje de usuario final incluido, Malas credenciales, malos parametros, prescripcion en estado incorrecto, etc.
                if (wsex != null)
                {
                    Console.WriteLine("El API de DrsBee retornó un error: " + wsex.Message + " de tipo " + wsex.Type);
                }
                // Error HTTP fuera drsbee, falla de conexion, timeout,etc.
                else if (hex != null)
                {
                    Console.WriteLine("Hubo un error de comunicación http con DrsBee, código: " + hex.HttpCode + "  invocando el URL: " + hex.Url);
                }
                else
                {
                    Console.WriteLine("Ocurrió un error desconocido ");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}
