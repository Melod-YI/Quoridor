package model.impl;

import static org.junit.Assert.*;

import org.junit.Before;
import org.junit.Test;

public class ChessBoardModelImplTest {

	@Before
	public void setUp() throws Exception {
	}

	@Test
	public void test() {
		ChessBoardModelImpl cbm = new ChessBoardModelImpl();
		assertTrue(cbm.initialize(9, 9, 10, 3));
//		cbm.print();
	}

}
