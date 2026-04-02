namespace ExceptionTests;

[TestClass]
public class GetElements_Should
{
    [TestMethod]
    [ExpectedException(typeof(IndexTooLongException))]
    public void ThrowIndexTooLongException_WhenIndex11()
    {
        //Given
        Worker worker = new Worker();

        //When
        worker.GetElement("11");

        //Then
        Assert.Fail("il y aurait dut y avoir une exception.");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidStringException))]
    public void ThrowInvalidStringException_Whentoto()
    {
        //Given
        Worker worker = new Worker();

        //When
        worker.GetElement("toto");

        //Then
        Assert.Fail("il y aurait dut y avoir une exception.");
    }
}